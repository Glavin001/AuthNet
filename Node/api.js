var locks = require('./locks');

// Twilio
var accountSid = process.env.TWILIO_ACCOUNT_SID;
var authToken = process.env.TWILIO_AUTH_TOKEN;
var client = require('twilio')(accountSid, authToken);
var async = require('async');
var _ = require('lodash');

console.log("Twilio Account Sid:", accountSid, ", authToken", authToken)

    // Create (send) an SMS message
    // POST /2010-04-01/Accounts/ACCOUNT_SID/SMS/Messages
    // "create" and "update" aliases are in place where appropriate on PUT and POST requests
    /*
    client.sms.messages.post({
        to:'+19022257035',
        from:'+19027071181',
        body: message
    }, function(err, text) {
        if (err) {
            console.log(err);
            return;
        }
        console.log('You sent: '+ text.body);
        console.log('Current status of this text message is: '+ text.status);
    });
    */

module.exports = {

    // shouldUnlock: function(lock, code, callback) {
    //     this.handleCode(lock, code, function(err, ) {
    //         callback();
    //     });
    // },

    handleCode: function(lock, code, callback) {
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
                    // Should unlock!
                    cb(null, true);
                } else {
                    // Create temp code
                    locks.createTempCode(codeDoc.lock, codeDoc.phone, codeDoc.name, function() {
                        cb(err, false);
                    });
                }
            }, function(err, results) {
                // console.log(err, results);
                callback(err, !_.every(results, function(v) {
                    return v === false;
                }));
            })

        });
    }

};