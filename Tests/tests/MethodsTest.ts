import assert = require('assert');
import MethodsClass from '../gen/TestSamples/MethodsClass';
import EnumFieldsClass from '../gen/TestSamples/EnumFieldsClass';
import { TranspiledIntEnum, TranspiledStringEnum } from '../gen/internal';

describe("Methods Test", () => {
    it("Test Hardcoded Method", () => {
        var obj = new MethodsClass({ MyValue: 8 });
        assert.equal(obj.MyValue, 8);
        assert.equal(obj.SetValue(12), 12);
        assert.equal(obj.MyValue, 12);
    });

    it("Test RPC Method", async () => {
        var obj = new MethodsClass({ MyValue: 8 });
        assert.equal(obj.MyValue, 8);
        assert.equal(await obj.SetValueRemote(12), 12);
        assert.equal(obj.MyValue, 12);
    }).timeout(10000); // Additional time to make http request

    it("Test async RPC Method", async () => {
        var obj = new MethodsClass({ MyValue: 8 });
        assert.equal(obj.MyValue, 8);
        assert.equal(await obj.SetValueRemoteAsync(12), 12);
        assert.equal(obj.MyValue, 12);
    }).timeout(10000); // Additional time to make http request

    it("Test RPC Method with service dependency", async () => {
        var obj = new MethodsClass({ MyValue: 8 });
        assert.equal(obj.MyValue, 8);
        assert.equal(await obj.SetValueDependencyInjection(12), 17);
        assert.equal(obj.MyValue, 12);
    }).timeout(10000); // Additional time to make http request

    it("Test RPC for enums", async () => {
        var obj = new EnumFieldsClass({});
        assert.equal(await obj.GetNextNTSE('NTSEValueA'), 'NTSEValueB');
        assert.equal(await obj.GetNextNTSE('NTSEValueB'), 'NTSEValueC');
        assert.equal(await obj.GetNextNTSE('NTSEValueC'), 'NTSEValueA');

        assert.equal(await obj.GetNextNTIE(3), 4);
        assert.equal(await obj.GetNextNTIE(4), 5);
        assert.equal(await obj.GetNextNTIE(5), 3);

        assert.equal(await obj.GetNextTSE(TranspiledStringEnum.TSEValueA), TranspiledStringEnum.TSEValueB);
        assert.equal(await obj.GetNextTSE(TranspiledStringEnum.TSEValueB), TranspiledStringEnum.TSEValueC);
        assert.equal(await obj.GetNextTSE(TranspiledStringEnum.TSEValueC), TranspiledStringEnum.TSEValueA);

        assert.equal(await obj.GetNextTIE(TranspiledIntEnum.TIEValueA), TranspiledIntEnum.TIEValueB);
        assert.equal(await obj.GetNextTIE(TranspiledIntEnum.TIEValueB), TranspiledIntEnum.TIEValueC);
        assert.equal(await obj.GetNextTIE(TranspiledIntEnum.TIEValueC), TranspiledIntEnum.TIEValueA);


    }).timeout(10000); // Additional time to make http request
});
