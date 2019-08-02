import assert = require('assert');
import MethodsClass from '../gen/TestSamples/MethodsClass';

describe("Methods Test", () => {
    it("Test Hardcoded Method", () => {
        var obj = new MethodsClass({ MyValue: 8 });
        assert.equal(obj.MyValue, 8);
        assert.equal(obj.SetValue(12), 12);
        assert.equal(obj.MyValue, 12);
    })
});
