var Datastore = require('nedb');
var path = require('path');

module.exports = function(databasePath) {

    var db = {
        "codes":  new Datastore({
            autoload: true,
            filename: (databasePath ? path.resolve(databasePath, "./codes.db") : undefined)
        })
    };

    return db;

};