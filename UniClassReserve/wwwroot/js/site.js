// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// AdminPanel: Classroom Edit (AJAX)
$(document).on('click', '.editClassroomSubmitBtn', function (e) {
    e.preventDefault();
    var classroomId = $(this).data('classroom-id');
    var $form = $('#editClassroomForm-' + classroomId);
    var data = $form.serialize();
    $.post('?handler=EditClassroom', data, function (result) {
        $('#editClassroomModal-' + classroomId).modal('hide');
        location.reload();
    }).fail(function (xhr) {
        alert('Error: ' + xhr.responseText);
    });
});

// AdminPanel: Classroom Delete (AJAX)
$(document).on('submit', '.deleteClassroomForm', function (e) {
    e.preventDefault();
    var $form = $(this);
    var classroomId = $form.find('button[type="submit"]').data('classroom-id') || $form.closest('form').attr('asp-route-id');
    if (confirm('Are you sure you want to delete this classroom?')) {
        var data = $form.serialize();
        // Fallback: get id from asp-route-id if not in form
        if (!data.includes('id=')) {
            var id = $form.attr('asp-route-id') || $form.closest('form').attr('asp-route-id');
            if (id) data += (data ? '&' : '') + 'id=' + id;
        }
        $.post('?handler=DeleteClassroom', data, function (result) {
            location.reload();
        }).fail(function (xhr) {
            alert('Error: ' + xhr.responseText);
        });
    }
});
