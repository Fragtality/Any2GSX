/**
 * Load a file as a blob
 * @param url The URL to load the file from.
 * @returns A promise that resolves with the blob.
 */
export declare function loadFileAsBlob(url: string): Promise<Blob>;
type Chunk = {
    name: string;
    data: Uint8Array;
};
type Chunks = Chunk[];
/**
 * https://github.com/hughsk/png-chunks-extract
 * Extract the data chunks from a PNG file.
 * Useful for reading the metadata of a PNG image, or as the base of a more complete PNG parser.
 * Takes the raw image file data as a Uint8Array or Node.js Buffer, and returns an array of chunks. Each chunk has a name and data buffer:
 * @param data {Uint8Array}
 * @returns {[{name: string, data: Uint8Array}]}
 */
export declare function extractChunks(data: Uint8Array): Chunks;
/**
 * https://github.com/hughsk/png-chunk-text
 * Reads a Uint8Array or Node.js Buffer instance containing a tEXt PNG chunk's data and returns its keyword/text:
 * @param data {Uint8Array}
 * @returns {{text: string, keyword: string}}
 */
export declare function textDecode(data: Uint8Array): {
    text: string;
    keyword: string;
};
interface Metadata {
    tEXt: {
        keyword: string;
    };
    pHYs: {
        x: number;
        y: number;
    };
}
/**
 * Get object with PNG metadata. only tEXt and pHYs chunks are parsed
 * @param buffer {Buffer}
 * @returns {{tEXt: {keyword: value}, pHYs: {x: number, y: number}}}
 */
export declare function readMetadata(buffer: ArrayBuffer): Metadata;
export {};
