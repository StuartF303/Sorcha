/**
 * QR Code JS Interop helper for Blazor WASM
 * Uses qrcode-generator library for client-side QR code generation
 */

const QrCodeHelper = {
    /**
     * Generates a QR code and renders it into the specified element as an img
     * @param {string} elementId - The ID of the container element
     * @param {string} text - The text/URL to encode
     * @param {number} size - The size in pixels (default: 256)
     */
    generate: function (elementId, text, size) {
        size = size || 256;
        const container = document.getElementById(elementId);
        if (!container) return;

        while (container.firstChild) {
            container.removeChild(container.firstChild);
        }

        const typeNumber = 0; // auto-detect
        const errorCorrection = 'M';
        const qr = qrcode(typeNumber, errorCorrection);
        qr.addData(text);
        qr.make();

        const cellSize = Math.floor(size / qr.getModuleCount());
        const margin = Math.floor((size - cellSize * qr.getModuleCount()) / 2);
        const dataUrl = qr.createDataURL(cellSize, margin);

        const img = document.createElement('img');
        img.src = dataUrl;
        img.alt = 'QR Code';
        img.width = size;
        img.height = size;
        container.appendChild(img);
    },

    /**
     * Copies text to the clipboard
     * @param {string} text - The text to copy
     * @returns {Promise<boolean>} Whether the copy succeeded
     */
    copyToClipboard: async function (text) {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch {
            // Fallback for older browsers
            const textArea = document.createElement('textarea');
            textArea.value = text;
            textArea.style.position = 'fixed';
            textArea.style.left = '-9999px';
            document.body.appendChild(textArea);
            textArea.select();
            const result = document.execCommand('copy');
            document.body.removeChild(textArea);
            return result;
        }
    }
};

window.QrCodeHelper = QrCodeHelper;
