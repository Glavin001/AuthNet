var assert = require("assert")

var db = require('../db');
var locks = require('../locks');
var api = require('../api');

// Fixtures
var fakeCode = {
    "phone": "15555555555",
    "code": "1234",
    "lock": "123",
    "name": "Tester Testerson",
    "temp": false
};

describe('Locks', function() {
    describe('#code()', function() {

        beforeEach(function(done) {
            db.codes.remove({}, {
                multi: true
            }, function(err, numRemoved) {
                if (err) {
                    return done(err);
                }
                db.codes.insert(fakeCode,
                    function(err,
                        newDoc) {
                        if (err) {
                            // console.error(
                            //     err);
                            done(err);
                            return;
                        }
                        done();
                    });
            });

        });


        it('should find code', function(done) {
            locks.getCode(fakeCode.lock, fakeCode.code,
                function(err, codeDoc) {
                    // console.log(err,
                    //     codeDoc);
                    if (err) {
                        assert(err)
                        done(err);
                    }
                    assert(true);
                    assert(codeDoc[0].phone ==
                        fakeCode.phone);
                    done();
                });
        })

        it('should validate code', function(done) {

            api.handleCode(fakeCode.lock, fakeCode.code,
                function(err, shouldUnlock) {
                    // console.log(err, shouldUnlock);
                    if (err) {
                        assert(err);
                        done(err);
                        return;
                    }
                    assert(true);
                    assert(shouldUnlock === false);
                    db.codes.find({
                        temp: true
                    }, function(err,
                        codeDocs) {
                        // console.log(
                        //     codeDocs);
                        assert(codeDocs.length ===
                            1);
                        var tempCodeDoc =
                            codeDocs[0];

                        api.handleCode(
                            tempCodeDoc
                            .lock,
                            tempCodeDoc
                            .code,
                            function(
                                err,
                                shouldUnlock
                            ) {
                                // console
                                //     .log(
                                //         err,
                                //         shouldUnlock
                                //     );
                                assert(shouldUnlock === true);
                                done();
                            });

                    });
                });

        });

    });
});