mergeInto(LibraryManager.library, {
  DevilsCut_RequestFileSystemSync: function () {
    try {
      if (typeof FS === "undefined" || typeof FS.syncfs !== "function") {
        console.warn("[Save][WebGL] FS.syncfs is not available.");
        return;
      }

      if (typeof Module === "undefined") {
        console.warn("[Save][WebGL] Module is not available.");
        return;
      }

      if (Module.DevilsCutSaveSyncInProgress) {
        Module.DevilsCutSaveSyncPending = true;
        return;
      }

      var runSync = function () {
        Module.DevilsCutSaveSyncInProgress = true;
        Module.DevilsCutSaveSyncPending = false;

        FS.syncfs(false, function (error) {
          Module.DevilsCutSaveSyncInProgress = false;

          if (error) {
            console.error("[Save][WebGL] FS.syncfs failed.", error);
          }

          if (Module.DevilsCutSaveSyncPending) {
            setTimeout(runSync, 0);
          }
        });
      };

      runSync();
    } catch (error) {
      if (typeof Module !== "undefined") {
        Module.DevilsCutSaveSyncInProgress = false;
      }

      console.error("[Save][WebGL] FS.syncfs request threw an exception.", error);
    }
  }
});
