// Twilio
var accountSid = process.env.TWILIO_ACCOUNT_SID;
var authToken = process.env.TWILIO_AUTH_TOKEN;
var phoneNumber = "+"+process.env.TWILIO_PHONE_NUMBER;
var client = require('twilio')(accountSid, authToken);
var async = require('async');
var _ = require('lodash');

console.log("Twilio Account Sid:", accountSid, ", authToken", authToken, ", number", phoneNumber);


module.exports = function(db) {
    var locks = require('./locks')(db);

    return {

        // shouldUnlock: function(lock, code, callback) {
        //     this.handleCode(lock, code, function(err, ) {
        //         callback();
        //     });
        // },

        handleCode: function(lock, code, callback) {
            var self = this;
            // Check if code exists
            locks.getCode(lock, code, function(err, codeDocs) {
                if (err) {
                    return callback(err, false);
                }

                // Check if any codes
                if (codeDocs.length === 0) {
                    // No codes found
                    return callback(null, false);
                }
                // Process all codeDocs
                async.map(codeDocs, function(codeDoc, cb) {
                    // console.log(codeDoc);
                    // Check if temp code
                    if (codeDoc.temp === true) {
                        // Is Temp code
                        // Delete temp code
                        locks.removeCode(codeDoc.lock, codeDoc.code, function() {
                            // Should unlock!
                            return cb(null, true);
                        });
                    } else {
                        // Create temp code
                        self.createTempCode(codeDoc, cb);
                    }
                }, function(err, results) {
                    // console.log(err, results);
                    callback(err, !_.every(results,
                        function(v) {
                            return v !== true;
                        }));
                })

            });
        },

        createTempCode: function(codeDoc, cb) {
            var self = this;
            locks.createTempCode(codeDoc.lock,
                codeDoc.phone, codeDoc.name,
                function(err,
                    newCodeDoc) {
                    if (err) {
                        return cb(err,
                            false);
                    }

                    // Send SMS to user's phone with new temp code
                    var message =
                        "Your temporary code is: " +
                        newCodeDoc.code;

                    self.sendSMS(
                        newCodeDoc.phone,
                        message,
                        function(
                            err,
                            text) {
                            cb(
                                err,
                                false
                            );
                        });

                });
        },

        sendSMS: function(to, message, callback) {

            // Create (send) an SMS message
            // POST /2010-04-01/Accounts/ACCOUNT_SID/SMS/Messages
            // "create" and "update" aliases are in place where appropriate on PUT and POST requests
            client.sms.messages.post({
                to: '+' + to,
                from: phoneNumber,
                body: message
            }, callback);

        }

    };

};