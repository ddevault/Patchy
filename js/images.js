var auto = true;
var delay = 5000;

$(document).ready(function () {
    $('.navright').click(function (e) {
        e.preventDefault();
        fadeNext();
        auto = false;
        return false;
    });
    $('.navleft').click(function (e) {
        e.preventDefault();
        fadePrev();
        auto = false;
        return false;
    });
    setTimeout(autoRotate, delay);
});

function fadeNext() {
    var parent = $('.outset');
    var currentIndex = $('.outset img.shown').index();
    var nextIndex = currentIndex + 1;
    if (nextIndex >= parent.children('img').length)
        nextIndex = 0;
    parent.children('img').eq(currentIndex).fadeOut(1000, function () {
        parent.children('img').eq(currentIndex).removeClass('shown');
    });
    parent.children('img').eq(nextIndex).fadeIn(1000, function () {
        parent.children('img').eq(nextIndex).addClass('shown');
    });
}

function fadePrev() {
    var parent = $('.outset');
    var currentIndex = $('.outset img.shown').index();
    var nextIndex = currentIndex - 1;
    if (nextIndex < 0)
        nextIndex = parent.children('img').length - 1;
    parent.children('img').eq(currentIndex).fadeOut(1000, function () {
        parent.children('img').eq(currentIndex).removeClass('shown');
    });
    parent.children('img').eq(nextIndex).fadeIn(1000, function () {
        parent.children('img').eq(nextIndex).addClass('shown');
    });
}

function autoRotate() {
    if (auto) {
        fadeNext();
        setTimeout(autoRotate, delay);
    }
}