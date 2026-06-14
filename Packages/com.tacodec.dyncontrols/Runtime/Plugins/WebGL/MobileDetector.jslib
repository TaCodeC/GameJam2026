// JavaScript plugin used by Unity WebGL through [DllImport("__Internal")].

mergeInto(LibraryManager.library, {

    // Returns 1 for a touch-oriented device and 0 otherwise.
    IsMobileBrowser: function() {
        var hasTouch = navigator.maxTouchPoints > 0;
        var isCoarse = matchMedia("(pointer: coarse)").matches;

        // Touch plus a coarse primary pointer usually means a phone or tablet.
        return (hasTouch && isCoarse) ? 1 : 0;
    }
});
