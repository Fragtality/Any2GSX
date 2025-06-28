/**
 * Load a file as a blob
 * @param url The URL to load the file from.
 * @returns A promise that resolves with the blob.
 */
export function loadFileAsBlob(url) {
    return new Promise((resolve, reject) => {
        const xhr = new XMLHttpRequest();
        xhr.open('GET', url, true);
        xhr.responseType = 'blob';
        xhr.onload = function (e) {
            if (this.status === 200) {
                resolve(this.response);
                // myBlob is now the blob that the object URL pointed to.
            }
            else {
                reject(this.response);
            }
        };
        xhr.send();
    });
}
Blob.prototype.arrayBuffer = function () {
    return new Promise((resolve, reject) => {
        // Blob -> ArrayBuffer
        const fileReader = new FileReader();
        fileReader.onload = function (event) {
            var _a;
            const arrayBuffer = (_a = event.target) === null || _a === void 0 ? void 0 : _a.result;
            if (typeof arrayBuffer === 'string' || arrayBuffer === null || arrayBuffer === undefined) {
                reject('ArrayBuffer is null');
                return;
            }
            resolve(arrayBuffer);
            // warn if read values are not the same as the original values
            // arrayEqual from: http://stackoverflow.com/questions/3115982/how-to-check-javascript-array-equals
            // function arrayEqual(a, b) {
            //   return !(a < b || b < a);
            // }
            // if (arrayBufferNew.byteLength !== arrayBuffer.byteLength) // should be 3
            //   reject("ArrayBuffer byteLength does not match");
            // if (arrayEqual(uint8ArrayNew, uint8Array) !== true) // should be [1,2,3]
            //   reject("Uint8Array does not match");
        };
        fileReader.readAsArrayBuffer(this);
        fileReader.result; // also accessible this way once the blob has been read
    });
};
const uint8 = new Uint8Array(4);
// const int32 = new Int32Array(uint8.buffer);
const uint32 = new Uint32Array(uint8.buffer);
/**
 * https://github.com/hughsk/png-chunks-extract
 * Extract the data chunks from a PNG file.
 * Useful for reading the metadata of a PNG image, or as the base of a more complete PNG parser.
 * Takes the raw image file data as a Uint8Array or Node.js Buffer, and returns an array of chunks. Each chunk has a name and data buffer:
 * @param data {Uint8Array}
 * @returns {[{name: string, data: Uint8Array}]}
 */
export function extractChunks(data) {
    if (data[0] !== 0x89)
        throw new Error('Invalid .png file header');
    if (data[1] !== 0x50)
        throw new Error('Invalid .png file header');
    if (data[2] !== 0x4e)
        throw new Error('Invalid .png file header');
    if (data[3] !== 0x47)
        throw new Error('Invalid .png file header');
    if (data[4] !== 0x0d)
        throw new Error('Invalid .png file header: possibly caused by DOS-Unix line ending conversion?');
    if (data[5] !== 0x0a)
        throw new Error('Invalid .png file header: possibly caused by DOS-Unix line ending conversion?');
    if (data[6] !== 0x1a)
        throw new Error('Invalid .png file header');
    if (data[7] !== 0x0a)
        throw new Error('Invalid .png file header: possibly caused by DOS-Unix line ending conversion?');
    let ended = false;
    const chunks = [];
    let idx = 8;
    while (idx < data.length) {
        // Read the length of the current chunk,
        // which is stored as a Uint32.
        uint8[3] = data[idx++];
        uint8[2] = data[idx++];
        uint8[1] = data[idx++];
        uint8[0] = data[idx++];
        // Chunk includes name/type for CRC check (see below).
        const length = uint32[0] + 4;
        const chunk = new Uint8Array(length);
        chunk[0] = data[idx++];
        chunk[1] = data[idx++];
        chunk[2] = data[idx++];
        chunk[3] = data[idx++];
        // Get the name in ASCII for identification.
        const name = String.fromCharCode(chunk[0]) +
            String.fromCharCode(chunk[1]) +
            String.fromCharCode(chunk[2]) +
            String.fromCharCode(chunk[3]);
        // The IHDR header MUST come first.
        if (!chunks.length && name !== 'IHDR') {
            throw new Error('IHDR header missing');
        }
        // The IEND header marks the end of the file,
        // so on discovering it break out of the loop.
        if (name === 'IEND') {
            ended = true;
            chunks.push({
                name: name,
                data: new Uint8Array(0),
            });
            break;
        }
        // Read the contents of the chunk out of the main buffer.
        for (let i = 4; i < length; i++) {
            chunk[i] = data[idx++];
        }
        // Read out the CRC value for comparison.
        // It's stored as an Int32.
        uint8[3] = data[idx++];
        uint8[2] = data[idx++];
        uint8[1] = data[idx++];
        uint8[0] = data[idx++];
        // const crcActual = int32[0];
        // const crcExpect = crc32.buf(chunk);
        // if (crcExpect !== crcActual) {
        // 	throw new Error('CRC values for ' + name + ' header do not match, PNG file is likely corrupted');
        // }
        // The chunk data is now copied to remove the 4 preceding
        // bytes used for the chunk name/type.
        const chunkData = new Uint8Array(chunk.buffer.slice(4));
        chunks.push({
            name: name,
            data: chunkData,
        });
    }
    if (!ended) {
        throw new Error('.png file ended prematurely: no IEND header was found');
    }
    return chunks;
}
/**
 * https://github.com/hughsk/png-chunk-text
 * Reads a Uint8Array or Node.js Buffer instance containing a tEXt PNG chunk's data and returns its keyword/text:
 * @param data {Uint8Array}
 * @returns {{text: string, keyword: string}}
 */
export function textDecode(data) {
    let naming = true;
    let text = '';
    let name = '';
    for (const code of data) {
        if (naming) {
            if (code) {
                name += String.fromCharCode(code);
            }
            else {
                naming = false;
            }
        }
        else {
            if (code) {
                text += String.fromCharCode(code);
            }
            else {
                throw new Error('Invalid NULL character found. 0x00 character is not permitted in tEXt content');
            }
        }
    }
    return {
        keyword: name,
        text: text,
    };
}
/**
 * read 4 bytes number from UInt8Array.
 * @param uint8array
 * @param offset
 * @returns {number}
 */
function readUint32(uint8array, offset) {
    const byte1 = uint8array[offset++];
    const byte2 = uint8array[offset++];
    const byte3 = uint8array[offset++];
    const byte4 = uint8array[offset];
    return 0 | (byte1 << 24) | (byte2 << 16) | (byte3 << 8) | byte4;
}
/**
 * Get object with PNG metadata. only tEXt and pHYs chunks are parsed
 * @param buffer {Buffer}
 * @returns {{tEXt: {keyword: value}, pHYs: {x: number, y: number}}}
 */
export function readMetadata(buffer) {
    const result = {
        tEXt: {
            keyword: '',
        },
        pHYs: { x: 0, y: 0 },
    };
    const chunks = extractChunks(new Uint8Array(buffer));
    chunks.forEach((chunk) => {
        if (chunk.name === 'tExt') {
            const textChunk = textDecode(chunk.data);
            result.tEXt[textChunk.keyword] = textChunk.text;
        }
        else if (chunk.name === 'pHYs') {
            result.pHYs = {
                x: readUint32(chunk.data, 0),
                y: readUint32(chunk.data, 4),
            };
        }
        else {
            result[chunk.name] = true;
        }
    });
    return result;
}
