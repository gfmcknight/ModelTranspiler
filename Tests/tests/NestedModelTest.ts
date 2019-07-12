import assert = require('assert');
import Class4 from '../gen/TestSamples/Class4';
import Class5 from '../gen/TestSamples/Class5';

describe("Test Nested Models", () => {
    it("Test Simple Property", () => {
        var obj = new Class4({ Class1Prop: { MyNumber: 5, MyOtherProp: 3 } });
        assert.equal(obj.Class1Prop.MyNumber, 5);
        assert.equal(obj.Class1Prop.MyOtherProp, 3);
    });

    it("Test Model-Specific Property", () => {
        var obj = new Class5({ Class2Prop: { differentNumberName: 2 } });
        assert.equal(obj.Class2Prop.MyNumber, 2);
    });
});
