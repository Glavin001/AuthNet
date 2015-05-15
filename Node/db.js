var Datastore = require('nedb');


var db = {
    "codes":  new Datastore({
        autoload: true
    })
};

module.exports = db;