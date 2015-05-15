var express = require('express');
var bodyParser = require('body-parser');
var app = express();
var db = require('./db');

// for parsing application/x-www-form-urlencoded
app.use(bodyParser.urlencoded({extended:true}));

app.post('/code', function(req, res) {
    console.log(req.body, req.query, req.params);
    var code = req.body.code;
    var rooom = req.body.room;
    
    res.send('Sent!');

});

app.listen(3000);

