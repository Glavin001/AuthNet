var Datastore = require('nedb');
var path = require('path');

module.exports = function(databasePath) {

    var db = {
        "codes":  new Datastore({
            autoload: true,
            filename: (databasePath ? path.resolve(databasePath, "./codes.db") : undefined)
        }),
        "logs":  new Datastore({
            autoload: true,
            filename: (databasePath ? path.resolve(databasePath, "./logs.db") : undefined)
        })
    };

    return db;

};