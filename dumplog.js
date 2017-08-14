const fs = require('fs');
const csv = require('csv-parser');

const files = {};
const dlls = {};

function rowHandler(data) {
    if (data.Path.match(/.dll$/) && data['Process Name'] === 'VBCSCompiler.exe') {
        if (files[data.Path]) {
            files[data.Path]++;
        } else {
            files[data.Path] = 1;
        }
    }
}

function processLines() {
    Object.keys(files).forEach(k => {
        var m = k.match(/\\([^$\\]+)$/)[1];
        if (dlls[m] && dlls[m] !== k) {
            console.log(`OLD: ${dlls[m]}, NEW: ${k}`);
        } else {
            dlls[m] = k;
        }
        return m;
    });
    const json = JSON.stringify(Object.keys(dlls).map(k => dlls[k]), null, 4);
    console.log(json);
}

fs.createReadStream('./Logfile.CSV')
    .pipe(csv())
    .on('data', rowHandler)
    .on('end', processLines)
    ;