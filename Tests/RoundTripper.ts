import * as fs from 'fs'
import * as readline from 'readline'

var IMPORT_PREFIX = './gen/';
var IMPORT_SUFFIX = '.js';

var INSTANCE_PREFIX = '../roundtrip/'
var INSTANCE_SUFFIX = '.json'

var reader = readline.createInterface(fs.createReadStream('../roundtrip-classes.txt'))

reader.on('line', (line: string) => {
    var className = IMPORT_PREFIX + line.replace('.', '/') + IMPORT_SUFFIX
    import(className).then(cls => {
        fs.readFile(INSTANCE_PREFIX + line + INSTANCE_SUFFIX, (readErr, data) => {
            if (readErr) {
                throw readErr;
            }

            var jsonData = JSON.parse(data.toString());
            var instance = new cls['default'](jsonData);

            fs.writeFile(INSTANCE_PREFIX + line + "-fromjs" + INSTANCE_SUFFIX,
                         JSON.stringify(instance), writeErr => {
                if (writeErr) {
                    throw writeErr;
                }
            });
        })

    }).catch(e => {
        console.log(e);
    });
})