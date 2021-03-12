/*
 * Copyright (c) 2021, Ferib Hellscream
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the 
 * "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, 
 * sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following 
 * conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
 * WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS 
 * OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR 
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *
 */

// NOTE: feel free to copy paste this piece of code!

var API = function () {
    // TODO: add settings?

    let lastResult = "N/A";

    let _decompileDone = function (result, callback) {
        lastResult = result;
        if (result == null || result.status == "Error") {
            callback();
            return;
        }
        callback();
    }

    let _decompileFile = function (luacFile, callback) {
        var fd = new FormData();

        // images
        fd.append('file', luacFile);

        console.log("Started Request");

        $.ajax({
            url: '/api/decompile/',
            type: 'post',
            data: fd,
            contentType: false,
            processData: false,
            timeout: 10000,
            success: function (data) { _decompileDone(data, callback) },
            error: function (data) { _decompileDone(null, callback) }
        });
    }

    let _getLastResult = function () {
        return lastResult;
    }

    return {
        decompileFile: _decompileFile,
        getLastResult: _getLastResult
    }
}();