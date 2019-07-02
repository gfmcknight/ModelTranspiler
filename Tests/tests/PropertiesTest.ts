﻿import assert = require('assert');
import Class1 from '../gen/TestSamples/Class1';
import Class2 from '../gen/TestSamples/Class2';
import AllValueTypesClass from '../gen/TestSamples/AllValueTypesClass';

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

    it("Test JsonIgnored Property", () => {
        var obj = new Class2({ "MyOtherProp": 6 });
        try {
            eval("obj.MyOtherProp");
            assert.fail("Error should have been thrown");
        } catch (error) { }
    });

    it("Test JsonProperty Property", () => {
        var obj = new Class2({ "differentNumberName": 4 });
        assert.equal(obj.MyNumber, 4, "Property should be accessible with declared name");
    });

    it("Test DateTime Property", () => {
        var obj = new AllValueTypesClass({ "DateTimeProperty": "2019-06-26T11:11:11" });
        assert.equal(obj.DateTimeProperty.getDate(), 26);
        assert.equal(obj.DateTimeProperty.getMonth(), 5);
        assert.equal(obj.DateTimeProperty.getFullYear(), 2019);
    });
});
