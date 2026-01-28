// Clipboard interop functions for Blazor WASM

/**
 * Copies text to the clipboard using the Clipboard API.
 * @param {string} text - The text to copy to clipboard.
 * @returns {Promise<boolean>} True if copy succeeded, false otherwise.
 */
window.clipboardInterop = {
    copyText: async function (text) {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch (err) {
            console.error('Failed to copy text: ', err);
            // Fallback for older browsers
            try {
                const textArea = document.createElement('textarea');
                textArea.value = text;
                textArea.style.position = 'fixed';
                textArea.style.left = '-9999px';
                textArea.style.top = '-9999px';
                document.body.appendChild(textArea);
                textArea.focus();
                textArea.select();
                const result = document.execCommand('copy');
                document.body.removeChild(textArea);
                return result;
            } catch (fallbackErr) {
                console.error('Fallback copy failed: ', fallbackErr);
                return false;
            }
        }
    }
};
