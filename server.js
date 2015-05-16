#!/usr/bin/env node

var express = require('express');
var bodyParser = require('body-parser');
var app = express();
var program = require('commander');
var pkg = require('./package.json');

program
  .version(pkg.vesion)
  .option('--db [directory]', 'Specify the database directory', './.tmp')
  .parse(process.argv);

var db = require('./db')(program.db);
var api = require('./api')(db);
var locks = require('./locks')(db);

// for parsing application/x-www-form-urlencoded
app.use(bodyParser.urlencoded({extended:true}));
// parse application/json
app.use(bodyParser.json());

// Public static content
app.use(express.static('public'));

app.post('/submit-code', function(req, res) {
    // console.log(req.body, req.query, req.params);
    var code = req.body.code;
    var lock = req.body.lock;
    api.handleCode(lock, code, function(err, shouldUnlock) {
        locks.getCode(lock, code, function(err, codes) {
            res.send(shouldUnlock ? "Unlock" : ( (codes.length > 0) ? "Correct" : "Wrong"));
        });
    });
});

app.get('/code', function(req, res) {
    db.codes.find({}, function(err, codes) {
        if (err) {
            return res.send(err.message);
        }
        res.json(codes);
    });
});

app.post('/code', function(req, res) {
    var body = req.body;
    // console.log('code', body);
    locks.createCode(body.lock, body.phone, body.name, !!body.isTemp, function(err, codeDoc) {
        res.json(codeDoc);
    });
});

app.delete('/code', function(req, res) {
    db.codes.remove(req.body, function(err, results) {
        if (err) {
            return res.send(err.message);
        }
        res.json(results);
    });
});

app.get('/logs', function(req, res) {
    db.logs.find({}).sort({ time: -1 })
    .exec(function(err, results) {
        if (err) {
            return res.send(err);
        }
        res.json(results);
    })
})

app.listen(process.env.PORT || 3000);

