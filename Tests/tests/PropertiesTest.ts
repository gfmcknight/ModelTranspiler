import assert = require('assert');
import Class1 from '../gen/Class1';

describe("Test Suite 1", () => {
    it("Test Set Property", () => {
        var obj = new Class1({});
        obj.MyOtherProp = 5;
        assert.equal(obj.MyOtherProp, 5, "Should be able to set non-readonly properties");
    });
});
