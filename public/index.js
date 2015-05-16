$(document).ready(function() {

    var $codesOutput = $('#codes-output');
    var $codesTemplate = $("#codes-template");

    function getCodes() {
        return $.getJSON('/code')
    }

    function displayCodes(codes) {
        var context = {
            codes: codes
        }
        console.log(context);
        var source = $codesTemplate.html();
        var template = Handlebars.compile(source);
        var html = template(context);
        $codesOutput.html(html);

        // Refresh refs
        var $addCodeForm = $('.add-code-form');
        var $removeCodeBtn = $('.remove-code-btn');
        var $refreshCodesBtn = $('.refresh-codes-btn');

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

    function refreshCodes(cb) {
        console.log('refreshCodes');
        getCodes()
            .then(function(codes) {
                displayCodes(codes);
            });
    }

    function addCode(name, lock, phone) {
        console.log('addCode', name, lock, phone);
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

});