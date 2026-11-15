const handlers = new WeakMap();
export function open(dialog, reference) {
    const previous = document.activeElement;
    const cancel = event => { event.preventDefault(); reference.invokeMethodAsync('Close').catch(() => {}); };
    dialog.addEventListener('cancel', cancel);
    handlers.set(dialog, { cancel, previous });
    dialog.showModal();
}
export function close(dialog) {
    const handler = handlers.get(dialog);
    if (handler) { dialog.removeEventListener('cancel', handler.cancel); handler.previous?.focus(); handlers.delete(dialog); }
    dialog.close();
}
