// Splitter drag handler — runs entirely in JS for smooth 60fps resizing,
// calls back to .NET only on drag-start and drag-end.
window.splitterDrag = {
    start: function (startX, startWidthPercent, dotNetRef) {
        const container = document.querySelector('.chat-main-content');
        const chatPanel = document.querySelector('.chat-panel');
        if (!container || !chatPanel) return;

        const containerRect = container.getBoundingClientRect();
        dotNetRef.invokeMethodAsync('OnSplitterDragStart');

        function onMove(e) {
            e.preventDefault();
            const x = e.clientX - containerRect.left;
            const percent = (x / containerRect.width) * 100;
            const clamped = Math.min(75, Math.max(15, percent));
            chatPanel.style.width = clamped.toFixed(1) + '%';
        }

        function onUp(e) {
            document.removeEventListener('pointermove', onMove);
            document.removeEventListener('pointerup', onUp);
            // Read the final width
            const finalWidth = parseFloat(chatPanel.style.width) || startWidthPercent;
            dotNetRef.invokeMethodAsync('OnSplitterDragEnd', finalWidth);
        }

        document.addEventListener('pointermove', onMove);
        document.addEventListener('pointerup', onUp);
    }
};
