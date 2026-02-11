/**
 * Token lifecycle management — tab visibility change detection.
 * Calls back into .NET via DotNetObjectReference when the tab becomes visible,
 * allowing the TokenRefreshService to check and refresh stale tokens.
 */

const TokenLifecycle = {
    _dotNetRef: null,
    _handler: null,

    /**
     * Registers the visibility change listener.
     * @param {DotNetObjectReference} dotNetRef - .NET object reference for callbacks
     */
    register: function (dotNetRef) {
        this._dotNetRef = dotNetRef;

        this._handler = () => {
            if (document.visibilityState === 'visible' && this._dotNetRef) {
                this._dotNetRef.invokeMethodAsync('OnTabVisible');
            }
        };

        document.addEventListener('visibilitychange', this._handler);
    },

    /**
     * Unregisters the visibility change listener and cleans up.
     */
    unregister: function () {
        if (this._handler) {
            document.removeEventListener('visibilitychange', this._handler);
            this._handler = null;
        }
        this._dotNetRef = null;
    }
};

window.TokenLifecycle = TokenLifecycle;
