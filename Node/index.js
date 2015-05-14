var express = require('express');
var bodyParser = require('body-parser');
var app = express();
app.use(bodyParser.urlencoded({extended:true})); // for parsing application/x-www-form-urlencoded

var accountSid = process.env.TWILIO_ACCOUNT_SID;
var authToken = process.env.TWILIO_AUTH_TOKEN;
var client = require('twilio')(accountSid, authToken);

console.log(accountSid, authToken)

app.post('/sms', function(req, res) {
    console.log(req.body, req.query, req.params);
    var message = "Your code is: "+req.body.code;
    // Create (send) an SMS message
    // POST /2010-04-01/Accounts/ACCOUNT_SID/SMS/Messages
    // "create" and "update" aliases are in place where appropriate on PUT and POST requests
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
    res.send('Sent!');
    
});

app.listen(3000);

