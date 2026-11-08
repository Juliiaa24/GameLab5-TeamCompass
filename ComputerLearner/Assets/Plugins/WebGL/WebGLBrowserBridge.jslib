mergeInto(LibraryManager.library, {

    WebGL_DownloadFile: function (filenamePtr, mimeTypePtr, bytePtr, byteLength) {
        try {
            var filename = UTF8ToString(filenamePtr);
            var mimeType = UTF8ToString(mimeTypePtr) || "application/octet-stream";
            var bytes = new Uint8Array(byteLength);
            bytes.set(HEAPU8.subarray(bytePtr, bytePtr + byteLength));

            var blob = new Blob([bytes], { type: mimeType });
            var url = URL.createObjectURL(blob);

            var link = document.createElement("a");
            link.href = url;
            link.download = filename;
            link.style.display = "none";
            document.body.appendChild(link);
            link.click();

            setTimeout(function () {
                if (link.parentNode) {
                    document.body.removeChild(link);
                }
                URL.revokeObjectURL(url);
            }, 2500);
        } catch (err) {
            console.error("[WebGLBrowserBridge] WebGL_DownloadFile failed:", err);
        }
    },

    WebGL_SyncFileSystem: function () {
        try {
            if (typeof FS !== "undefined" && typeof FS.syncfs === "function") {
                FS.syncfs(false, function (err) {
                    if (err) {
                        console.warn("[WebGLBrowserBridge] FS.syncfs error:", err);
                    }
                });
            }
        } catch (err) {
            console.warn("[WebGLBrowserBridge] WebGL_SyncFileSystem exception:", err);
        }
    },

    WebGL_InstallBrowserInterceptors: function () {
        if (window.__computerLearnerInterceptorsInstalled) {
            return;
        }
        window.__computerLearnerInterceptorsInstalled = true;

        var syncFsSafely = function () {
            try {
                if (typeof FS !== "undefined" && typeof FS.syncfs === "function") {
                    FS.syncfs(false, function () {});
                }
            } catch (e) {}
        };

        // Prevent browser hijacking of Ctrl/Cmd + S, P, F, G, U while allowing Unity Input System to receive the keydown
        window.addEventListener("keydown", function (e) {
            if (e.ctrlKey || e.metaKey) {
                var key = (e.key || "").toLowerCase();
                if (key === "s" || key === "p" || key === "f" || key === "g" || key === "u" ||
                    e.keyCode === 83 || e.keyCode === 80 || e.keyCode === 70 || e.keyCode === 71 || e.keyCode === 85) {
                    e.preventDefault();
                }
            }
        }, { capture: true, passive: false });

        // Prevent native browser right-click context menu on the WebGL canvas / page
        window.addEventListener("contextmenu", function (e) {
            e.preventDefault();
        }, { capture: true, passive: false });

        // Persist IndexedDB when tab loses visibility or before unload
        document.addEventListener("visibilitychange", function () {
            if (document.visibilityState === "hidden") {
                syncFsSafely();
            }
        });

        window.addEventListener("beforeunload", function () {
            syncFsSafely();
        });
    }

});
