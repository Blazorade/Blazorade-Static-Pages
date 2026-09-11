export function removeStaticMetadata() {
    document.head
        .querySelectorAll('[data-blazorade-static-metadata]')
        .forEach(element => element.remove());
}