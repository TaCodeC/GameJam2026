// JavaScript plugin used by Unity WebGL through [DllImport("__Internal")].

mergeInto(LibraryManager.library, {

    // Returns 1 for a touch-oriented device and 0 otherwise.
    IsMobileBrowser: function() {
        function matches(query) {
            return typeof matchMedia === "function" && matchMedia(query).matches;
        }

        function readParamOverride(searchOrHash) {
            if (!searchOrHash || typeof URLSearchParams !== "function") {
                return null;
            }

            var query = searchOrHash.charAt(0) === "#" ? searchOrHash.substring(1) : searchOrHash;
            var params = new URLSearchParams(query);
            var value =
                params.get("dyncontrolsMobile") ||
                params.get("mobileControls") ||
                params.get("mobile") ||
                params.get("touchControls") ||
                params.get("device") ||
                params.get("platform") ||
                params.get("itchio_device");

            if (value === null) {
                return null;
            }

            value = value.toLowerCase();

            if (/^(1|true|yes|on|mobile|touch|phone|tablet|android|ios|iphone|ipad)$/.test(value)) {
                return true;
            }

            if (/^(0|false|no|off|pc|desktop|web|browser|windows|mac|linux)$/.test(value)) {
                return false;
            }

            return null;
        }

        function readExplicitOverride() {
            var locations = [window.location.search, window.location.hash];

            try {
                if (window.parent && window.parent !== window) {
                    locations.push(window.parent.location.search);
                    locations.push(window.parent.location.hash);
                }
            } catch (error) {
                // Cross-origin iframe parents are expected on portals such as itch.io.
            }

            for (var i = 0; i < locations.length; i++) {
                var override = readParamOverride(locations[i]);
                if (override !== null) {
                    return override;
                }
            }

            return null;
        }

        var explicitOverride = readExplicitOverride();
        if (explicitOverride !== null) {
            return explicitOverride ? 1 : 0;
        }

        var userAgent = (navigator.userAgent || navigator.vendor || "").toLowerCase();
        var platform = (navigator.platform || "").toLowerCase();
        var maxTouchPoints = navigator.maxTouchPoints || navigator.msMaxTouchPoints || 0;
        var hasTouch = maxTouchPoints > 0 || "ontouchstart" in window;
        var hasDesktopPointer =
            matches("(pointer: fine)") ||
            matches("(any-pointer: fine)") ||
            matches("(hover: hover)") ||
            matches("(any-hover: hover)");

        if (navigator.userAgentData && navigator.userAgentData.mobile === true) {
            return 1;
        }

        var isMobileUserAgent = /android|iphone|ipod|iemobile|blackberry|bb10|mobile|tablet|kindle|silk|opera mini/.test(userAgent);
        var isIPadDesktopMode = platform.indexOf("mac") >= 0 && maxTouchPoints > 1 && /safari|applewebkit/.test(userAgent);

        if (isMobileUserAgent || isIPadDesktopMode) {
            return 1;
        }

        var isDesktopUserAgent = /windows nt|macintosh|x11|linux x86_64|cros|freebsd|openbsd/.test(userAgent);
        if (isDesktopUserAgent && hasDesktopPointer) {
            return 0;
        }

        var hasCoarsePointer = matches("(pointer: coarse)") || matches("(any-pointer: coarse)");
        var hasNoHover = matches("(hover: none)") || matches("(any-hover: none)");

        return (hasTouch && hasCoarsePointer && hasNoHover && !hasDesktopPointer) ? 1 : 0;
    }
});
