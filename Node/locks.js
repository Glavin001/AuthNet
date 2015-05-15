module.exports = function(db) {

    return {

        getCode: function(lock, code, callback) {
            db.codes.find({
                lock: lock,
                code: code.toString()
            }, callback);
        },

        createCode: function(lock, phone, name, isTemp, callback) {
            var code = Math.floor(100000 + Math.random() * 900000); // 6-digit code
            if (typeof isTemp === "function") {
                callback = isTemp;
                isTemp = false;
            }
            db.codes.insert({
                lock: lock,
                phone: phone,
                name: name,
                code: code.toString(),
                temp: isTemp
            }, callback)
        },

        createTempCode: function(lock, phone, name, callback) {
            this.createCode(lock, phone, name, true, callback);
        },

        removeCode: function(lock, code, callback) {
            db.codes.remove({
                lock: lock,
                code: code.toString()
            }, callback);
        }

        //, usersForCode: function(lock, code, callback) {
        //     this.getCode(lock, code, function(err, codes) {
        //         if (err) {
        //             return callback(err, []);
        //         }
        //         var users = _.map(codes, function(code) {
        //             return {
        //                 phone: code.phone,
        //                 name: code.name
        //             }
        //         });
        //         callback(null, users);
        //     });
        // }

    };

};
