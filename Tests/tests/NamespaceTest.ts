import assert = require('assert');
import Class3 from '../gen/TestSamples/SubNamespace/Class3';

describe("Test Namespaces", () => {
    it("SubNamespace", () => {
        // Just check that we can import the file
        var obj = new Class3({});
    });
});
