/**
 * Author: Diego & TeamCompass
 * Date: 13/10/2026
 * Description: Deprecated level 10 copy-paste task. Replaced by interactive Virtual Desktop Levels 10 to 16.
 */
using System;
using UnityEngine;

namespace ComputerLearning
{
    [Obsolete("Replaced by VirtualDesktopGuidedLevels")]
    public class CopyPasteTask : BaseTask
    {
        protected override void SetupTask(TaskDefinition definition)
        {
            Complete(true);
        }
    }
}
