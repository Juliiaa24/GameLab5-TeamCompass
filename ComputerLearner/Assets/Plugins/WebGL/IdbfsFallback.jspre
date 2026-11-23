// Ensures IDBFS is always defined in Unity 6 WebGL builds even if Emscripten
// strips libidbfs.js due to Windows directory junction/symlink path mismatch.
var IDBFS = (typeof IDBFS !== 'undefined') ? IDBFS : {
  dbs: {},
  indexedDB: function() {
    return indexedDB;
  },
  DB_VERSION: 21,
  DB_STORE_NAME: 'FILE_DATA',
  queuePersist: function(mount) {
    function onPersistComplete() {
      if (mount.idbPersistState === 'again') startPersist();
      else mount.idbPersistState = 0;
    }
    function startPersist() {
      mount.idbPersistState = 'idb';
      IDBFS.syncfs(mount, false, onPersistComplete);
    }
    if (!mount.idbPersistState) {
      mount.idbPersistState = setTimeout(startPersist, 0);
    } else if (mount.idbPersistState === 'idb') {
      mount.idbPersistState = 'again';
    }
  },
  mount: function(mount) {
    var mnt = MEMFS.mount(mount);
    if (mount && mount.opts && mount.opts.autoPersist) {
      mount.idbPersistState = 0;
      var memfs_node_ops = mnt.node_ops;
      mnt.node_ops = Object.assign({}, mnt.node_ops);
      mnt.node_ops.mknod = function(parent, name, mode, dev) {
        var node = memfs_node_ops.mknod(parent, name, mode, dev);
        node.node_ops = mnt.node_ops;
        node.idbfs_mount = mnt.mount;
        node.memfs_stream_ops = node.stream_ops;
        node.stream_ops = Object.assign({}, node.stream_ops);
        node.stream_ops.write = function(stream, buffer, offset, length, position, canOwn) {
          stream.node.isModified = true;
          return node.memfs_stream_ops.write(stream, buffer, offset, length, position, canOwn);
        };
        node.stream_ops.close = function(stream) {
          var n = stream.node;
          if (n.isModified) {
            IDBFS.queuePersist(n.idbfs_mount);
            n.isModified = false;
          }
          if (n.memfs_stream_ops.close) return n.memfs_stream_ops.close(stream);
        };
        IDBFS.queuePersist(mnt.mount);
        return node;
      };
      mnt.node_ops.rmdir = function(parent, name) { IDBFS.queuePersist(mnt.mount); return memfs_node_ops.rmdir(parent, name); };
      mnt.node_ops.symlink = function(parent, newname, oldpath) { IDBFS.queuePersist(mnt.mount); return memfs_node_ops.symlink(parent, newname, oldpath); };
      mnt.node_ops.unlink = function(parent, name) { IDBFS.queuePersist(mnt.mount); return memfs_node_ops.unlink(parent, name); };
      mnt.node_ops.rename = function(old_node, new_dir, new_name) { IDBFS.queuePersist(mnt.mount); return memfs_node_ops.rename(old_node, new_dir, new_name); };
    }
    return mnt;
  },
  syncfs: function(mount, populate, callback) {
    IDBFS.getLocalSet(mount, function(err, local) {
      if (err) return callback(err);
      IDBFS.getRemoteSet(mount, function(err, remote) {
        if (err) return callback(err);
        var src = populate ? remote : local;
        var dst = populate ? local : remote;
        IDBFS.reconcile(src, dst, callback);
      });
    });
  },
  quit: function() {
    Object.values(IDBFS.dbs).forEach(function(value) { value.close(); });
    IDBFS.dbs = {};
  },
  getDB: function(name, callback) {
    var db = IDBFS.dbs[name];
    if (db) return callback(null, db);
    var req;
    try {
      req = IDBFS.indexedDB().open(name, IDBFS.DB_VERSION);
    } catch (e) {
      return callback(e);
    }
    if (!req) return callback('Unable to connect to IndexedDB');
    req.onupgradeneeded = function(e) {
      var db = e.target.result;
      var transaction = e.target.transaction;
      var fileStore = db.objectStoreNames.contains(IDBFS.DB_STORE_NAME)
        ? transaction.objectStore(IDBFS.DB_STORE_NAME)
        : db.createObjectStore(IDBFS.DB_STORE_NAME);
      if (!fileStore.indexNames.contains('timestamp')) {
        fileStore.createIndex('timestamp', 'timestamp', { unique: false });
      }
    };
    req.onsuccess = function() {
      db = req.result;
      IDBFS.dbs[name] = db;
      callback(null, db);
    };
    req.onerror = function(e) {
      callback(e.target.error);
      e.preventDefault();
    };
  },
  getLocalSet: function(mount, callback) {
    var entries = {};
    function isRealDir(p) { return p !== '.' && p !== '..'; }
    function toAbsolute(root) { return function(p) { return PATH.join2(root, p); }; }
    var check = FS.readdir(mount.mountpoint).filter(isRealDir).map(toAbsolute(mount.mountpoint));
    while (check.length) {
      var path = check.pop();
      var stat;
      try {
        stat = FS.stat(path);
      } catch (e) {
        return callback(e);
      }
      if (FS.isDir(stat.mode)) {
        check.push.apply(check, FS.readdir(path).filter(isRealDir).map(toAbsolute(path)));
      }
      entries[path] = { timestamp: stat.mtime };
    }
    return callback(null, { type: 'local', entries: entries });
  },
  getRemoteSet: function(mount, callback) {
    var entries = {};
    IDBFS.getDB(mount.mountpoint, function(err, db) {
      if (err) return callback(err);
      try {
        var transaction = db.transaction([IDBFS.DB_STORE_NAME], 'readonly');
        transaction.onerror = function(e) {
          callback(e.target.error);
          e.preventDefault();
        };
        var store = transaction.objectStore(IDBFS.DB_STORE_NAME);
        var index = store.index('timestamp');
        index.openKeyCursor().onsuccess = function(event) {
          var cursor = event.target.result;
          if (!cursor) {
            return callback(null, { type: 'remote', db: db, entries: entries });
          }
          entries[cursor.primaryKey] = { timestamp: cursor.key };
          cursor.continue();
        };
      } catch (e) {
        return callback(e);
      }
    });
  },
  loadLocalEntry: function(path, callback) {
    var stat, node;
    try {
      var lookup = FS.lookupPath(path);
      node = lookup.node;
      stat = FS.stat(path);
    } catch (e) {
      return callback(e);
    }
    if (FS.isDir(stat.mode)) {
      return callback(null, { timestamp: stat.mtime, mode: stat.mode });
    } else if (FS.isFile(stat.mode)) {
      node.contents = MEMFS.getFileDataAsTypedArray(node);
      return callback(null, { timestamp: stat.mtime, mode: stat.mode, contents: node.contents });
    } else {
      return callback(new Error('node type not supported'));
    }
  },
  storeLocalEntry: function(path, entry, callback) {
    try {
      if (FS.isDir(entry['mode'])) {
        FS.mkdirTree(path, entry['mode']);
      } else if (FS.isFile(entry['mode'])) {
        FS.writeFile(path, entry['contents'], { canOwn: true });
      } else {
        return callback(new Error('node type not supported'));
      }
      FS.chmod(path, entry['mode']);
      FS.utime(path, entry['timestamp'], entry['timestamp']);
    } catch (e) {
      return callback(e);
    }
    callback(null);
  },
  removeLocalEntry: function(path, callback) {
    try {
      var stat = FS.stat(path);
      if (FS.isDir(stat.mode)) {
        FS.rmdir(path);
      } else if (FS.isFile(stat.mode)) {
        FS.unlink(path);
      }
    } catch (e) {
      return callback(e);
    }
    callback(null);
  },
  loadRemoteEntry: function(store, path, callback) {
    var req = store.get(path);
    req.onsuccess = function(event) { callback(null, event.target.result); };
    req.onerror = function(e) {
      callback(e.target.error);
      e.preventDefault();
    };
  },
  storeRemoteEntry: function(store, path, entry, callback) {
    try {
      var req = store.put(entry, path);
    } catch (e) {
      callback(e);
      return;
    }
    req.onsuccess = function() { callback(); };
    req.onerror = function(e) {
      callback(e.target.error);
      e.preventDefault();
    };
  },
  removeRemoteEntry: function(store, path, callback) {
    var req = store.delete(path);
    req.onsuccess = function() { callback(); };
    req.onerror = function(e) {
      callback(e.target.error);
      e.preventDefault();
    };
  },
  reconcile: function(src, dst, callback) {
    var total = 0;
    var create = [];
    Object.entries(src.entries).forEach(function(pair) {
      var key = pair[0];
      var e = pair[1];
      var e2 = dst.entries[key];
      if (!e2 || e['timestamp'].getTime() !== e2['timestamp'].getTime()) {
        create.push(key);
        total++;
      }
    });
    var remove = [];
    Object.keys(dst.entries).forEach(function(key) {
      if (!src.entries[key]) {
        remove.push(key);
        total++;
      }
    });
    if (!total) return callback(null);
    var errored = false;
    var db = src.type === 'remote' ? src.db : dst.db;
    var transaction = db.transaction([IDBFS.DB_STORE_NAME], 'readwrite');
    var store = transaction.objectStore(IDBFS.DB_STORE_NAME);
    function done(err) {
      if (err && !errored) {
        errored = true;
        return callback(err);
      }
    }
    transaction.onerror = transaction.onabort = function(e) {
      done(e.target.error);
      e.preventDefault();
    };
    transaction.oncomplete = function() {
      if (!errored) callback(null);
    };
    create.sort().forEach(function(path) {
      if (dst.type === 'local') {
        IDBFS.loadRemoteEntry(store, path, function(err, entry) {
          if (err) return done(err);
          IDBFS.storeLocalEntry(path, entry, done);
        });
      } else {
        IDBFS.loadLocalEntry(path, function(err, entry) {
          if (err) return done(err);
          IDBFS.storeRemoteEntry(store, path, entry, done);
        });
      }
    });
    remove.sort().reverse().forEach(function(path) {
      if (dst.type === 'local') {
        IDBFS.removeLocalEntry(path, done);
      } else {
        IDBFS.removeRemoteEntry(store, path, done);
      }
    });
  }
};
