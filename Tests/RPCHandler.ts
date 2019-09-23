const https = require('https')

// In order to debug, we need to accept bad or self-signed
// certificates to hit localhost
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

async function handleRPCRequest(thisObject: any, args: Array<any>,
    className: string, methodName: string) {

    const message = {
        ThisObject: thisObject,
        Values: args,
    }
    let messageData: string = JSON.stringify(message);

    const options = {
        hostname: 'localhost',
        port: 44322,
        path: `/rpc/${className}/${methodName}`,
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Content-Length': Buffer.byteLength(messageData),
        }
    };

    return new Promise((resolve, reject) => {
        const req = https.request(options, res => {
            if (res.statusCode != 200) {
                reject("Unexpected status code: " + res.statusCode);
                return;
            }

            res.setEncoding('utf8');
            let responseData: string = "";

            res.on('data', chunk => {
                responseData += chunk;
            });

            res.on('end', () => {
                resolve(JSON.parse(responseData));
            })

        });

        req.on('error', e => {
            reject(e);
        });

        req.write(messageData);
        req.end();
    });
}

export default { handleRPCRequest };