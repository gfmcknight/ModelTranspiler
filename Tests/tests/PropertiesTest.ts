import assert = require('assert');
import Class1 from '../gen/Class1';

describe("Test Properties", () => {
    it("Test Set Property", () => {
        var obj = new Class1({});
        obj.MyOtherProp = 5;
        assert.equal(obj.MyOtherProp, 5, "Should be able to set non-readonly properties");
    });

    it("Test Readonly Property", () => {
        var obj = new Class1({"MyNumber" : 3});
        try {
            eval("obj.MyNumber = 5;");
            assert.fail("Error should have been thrown.");
        } catch (error) { }

        assert.equal(obj.MyNumber, 3, "Should not be able to set non-readonly properties");
    });
});
