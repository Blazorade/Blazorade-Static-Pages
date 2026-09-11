import { removeStaticMetadata } from './staticMetadata.js';

export function beforeStart() {
    removeStaticMetadata();
}