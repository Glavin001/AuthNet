Handlebars.registerHelper('format-date', function(format, date) {
    return moment(date).format(format);
});

$(document).ready(function() {

    var $codesOutput = $('#codes-output');
    var $codesTemplate = $("#codes-template");
    var $logsOutput = $('#logs-output');
    var $logsTemplate = $("#logs-template");

    function getCodes() {
        return $.getJSON('/code')
    }

    function getLogs() {
        return $.getJSON('/logs')
    }

    function displayCodes(codes) {
        var context = {
            codes: codes
        };
        // console.log(context);
        var source = $codesTemplate.html();
        var template = Handlebars.compile(source);
        var html = template(context);
        $codesOutput.html(html);

        // Refresh refs
        var $addCodeForm = $('.add-code-form', $codesOutput);
        var $removeCodeBtn = $('.remove-code-btn', $codesOutput);
        var $refreshCodesBtn = $('.refresh-codes-btn', $codesOutput);

        $addCodeForm.submit(function(event) {
            var $nameInput = $('#name-input');
            var $phoneInput = $('#phone-input');
            var $lockInput = $('#lock-input');

            var name = $nameInput.val();
            var lock = $lockInput.val();
            var phone = $phoneInput.val();

            addCode(name, lock, phone)
                .then(refreshCodes)

            event.preventDefault();
        });

        $removeCodeBtn.click(function(event) {
            removeCode($(event.target).data('code-id'))
                .then(refreshCodes);
        });

        $refreshCodesBtn.click(refreshCodes);

    }

    function displayLogs(logs) {
        var context = {
            messages: logs.map(function(log) {
                if (log.label === "incorrect") {
                    log.classes = "list-group-item-danger";
                } else if (log.label === "unlock") {
                    log.classes = "list-group-item-success";
                } else if (log.label === "lock") {
                    log.classes = "list-group-item-warning";
                }
                return log;
            })
        };
        // console.log(context);
        var source = $logsTemplate.html();
        var template = Handlebars.compile(source);
        var html = template(context);
        $logsOutput.html(html);

        // Refresh refs
        var $refreshLogsBtn = $('.refresh-logs-btn', $logsOutput);

        $refreshLogsBtn.click(refreshLogs);
    }

    function refreshCodes(cb) {
        // console.log('refreshCodes');
        getCodes()
            .then(function(codes) {
                displayCodes(codes);
            });
    }

    function refreshLogs(cb) {
        // console.log('refreshLogs');
        getLogs()
            .then(function(logs) {
                displayLogs(logs);
            });
    }


    function addCode(name, lock, phone) {
        // console.log('addCode', name, lock, phone);
        return $.ajax({
            type: "POST",
            url: "/code",
            data: {
                name: name,
                lock: lock,
                phone: phone
            }
        });
    }

    function removeCode(id) {
        console.log('removeCode', id);
        return $.ajax({
            type: "DELETE",
            url: "/code",
            data: {
                _id: id
            }
        });
    }

    // Init
    refreshCodes();
    refreshLogs();

    setInterval(refreshLogs, 1000);

});