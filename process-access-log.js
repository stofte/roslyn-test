const fs = require('fs');
const csv = require('csv-parser');
const readline = require('readline');

const inputFileName = process.argv[2];
const outputFileName = process.argv[3];
const isWin32 = process.platform === 'win32';

const files = {};
const dlls = {};

function rowHandler(data) {
    if (typeof data === 'string') { // auditctl produced logs
        const m = data.match(/(\/home\/[\w\/\.]+.dll)\Wopen/);
        if (m) {
            if (files[m[1]]) {
                files[m[1]]++;
            } else {
                files[m[1]] = 1;
            }
        }
    } else if (data.Path.match(/(.dll)\b/) && (data['Process Name'] === 'VBCSCompiler.exe' || data['Process Name'] === 'dotnet.exe' )) {
        if (files[data.Path]) {
            files[data.Path]++;
        } else {
            files[data.Path] = 1;
        }
    }
}

function processLines() {
    let json = '';
    if (isWin32) {
        Object.keys(files).forEach(k => {
            var m = k.match(/\\([^$\\]+)$/)[1];
            if (dlls[m] && dlls[m] !== k) {
                // console.log(`OLD: ${dlls[m]}, NEW: ${k}`);
            } else {
                dlls[m] = k;
            }
            return m;
        });
        json = JSON.stringify(Object.keys(dlls).map(k => dlls[k]), null, 4);
    } else {
        json = JSON.stringify(Object.keys(files), null, 4);
    }
    fs.writeFile(outputFileName, json, (err) => {
        if (err) console.error(err);
    });
}

if (process.platform === 'win32') {
    fs.createReadStream(inputFileName)
        .pipe(csv())
        .on('data', rowHandler)
        .on('end', processLines);
} else {
    const rl = readline.createInterface({
        input: fs.createReadStream(inputFileName)
    })
    .on('line', rowHandler)
    .on('close', processLines);
}
