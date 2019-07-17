import assert = require('assert');
import Class6 from '../gen/TestSamples/Class6'

describe("Inheritance Tests", () => {
    it("Supertype Property Exists", () => {
        var obj = new Class6({ MyOtherProp: 6.5, AdditionalProperty: 12 });
        assert.equal(obj.MyOtherProp, 6.5);
        assert.equal(obj.AdditionalProperty, 12);
    });
});
