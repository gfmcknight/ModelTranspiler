import assert = require('assert');
import Class6 from '../gen/TestSamples/Class6'

import BaseType from '../gen/TestSamples/BaseType'
import SubTypeA from '../gen/TestSamples/SubTypeA'
import SubTypeB from '../gen/TestSamples/SubTypeB'
import SubASubTypeA from '../gen/TestSamples/SubASubTypeA'
import SubASubTypeB from '../gen/TestSamples/SubASubTypeB'




describe("Inheritance Tests", () => {
    it("Supertype Property Exists", () => {
        var obj = new Class6({ MyOtherProp: 6.5, AdditionalProperty: 12 });
        assert.equal(obj.MyOtherProp, 6.5);
        assert.equal(obj.AdditionalProperty, 12);
    });

    it("fromJSON Uses Discriminator", () => {
        var obj = BaseType.fromJSON({
            BaseValueField: '3e0dda98-3ba5-4893-9156-d677fbd1886e',
            BaseDiscriminatingField: "A",
            SubTypeAStringField: "Hello",
            SubTypeBIntField: 6
        });

        assert.ok(obj instanceof SubTypeA);
        assert.equal(obj["SubTypeAStringField"], "Hello");
        assert.equal(obj["SubTypeBIntField"], undefined);

        obj = BaseType.fromJSON({
            BaseValueField: '3e0dda98-3ba5-4893-9156-d677fbd1886e',
            BaseDiscriminatingField: "B",
            SubTypeAStringField: "Hello",
            SubTypeBIntField: 6
        });

        assert.ok(obj instanceof SubTypeB);
        assert.equal(obj["SubTypeAStringField"], undefined);
        assert.equal(obj["SubTypeBIntField"], 6);
    });

    it("fromJSON Allows Default", () => {
        var obj = BaseType.fromJSON({
            BaseValueField: '3e0dda98-3ba5-4893-9156-d677fbd1886e',
            BaseDiscriminatingField: "None",
            SubTypeAStringField: "Hello",
            SubTypeBIntField: 6
        });

        assert.ok(obj instanceof BaseType);
        assert.equal(obj["SubTypeAStringField"], undefined);
        assert.equal(obj["SubTypeBIntField"], undefined);
    });
});
