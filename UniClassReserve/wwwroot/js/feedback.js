document.addEventListener('DOMContentLoaded', function () {
    // Tüm feedback formlarını dinle
    document.querySelectorAll('form[data-feedback-ajax]').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            var modal = form.closest('.modal');
            var formData = new FormData(form);
            var submitBtn = form.querySelector('button[type="submit"]');
            if (submitBtn) submitBtn.disabled = true;
            fetch(form.action, {
                method: 'POST',
                body: formData,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            })
            .then(response => response.json())
            .then(data => {
                if (submitBtn) submitBtn.disabled = false;
                if (data.success) {
                    // Modalı kapat
                    if (window.bootstrap && modal) {
                        var bsModal = window.bootstrap.Modal.getInstance(modal);
                        if (bsModal) bsModal.hide();
                    } else if (modal) {
                        $(modal).modal('hide');
                    }
                    // Toast veya inline mesaj göster
                    showToast(data.message || 'Feedback submitted!', 'success');
                    // Feedback alanını güncelle (sayfa yenilemeden)
                    if (data.updatedHtml && data.feedbackContainerId) {
                        var container = document.getElementById(data.feedbackContainerId);
                        if (container) container.innerHTML = data.updatedHtml;
                    } else {
                        // Alternatif: sayfayı yenile
                        setTimeout(() => window.location.reload(), 1000);
                    }
                } else {
                    showToast(data.message || 'Error submitting feedback', 'danger');
                }
            })
            .catch(err => {
                if (submitBtn) submitBtn.disabled = false;
                showToast('Error submitting feedback', 'danger');
            });
        });
    });

    // Cancel butonlarına tıklandığında modalı aç
    document.querySelectorAll('button[data-cancel-reservation]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var reservationId = btn.getAttribute('data-cancel-reservation');
            var modal = document.getElementById('cancelReservationModal');
            if (modal) {
                modal.querySelector('input[name="reservationId"]').value = reservationId;
                if (window.bootstrap) {
                    var bsModal = new window.bootstrap.Modal(modal);
                    bsModal.show();
                } else {
                    $(modal).modal('show');
                }
            }
        });
    });
    // Modal içindeki onay butonuna tıklandığında AJAX ile iptal et
    var cancelForm = document.getElementById('cancelReservationForm');
    if (cancelForm) {
        cancelForm.addEventListener('submit', function (e) {
            e.preventDefault();
            var reservationId = cancelForm.querySelector('input[name="reservationId"]').value;
            var token = cancelForm.querySelector('input[name="__RequestVerificationToken"]').value;
            var submitBtn = cancelForm.querySelector('button[type="submit"]');
            if (submitBtn) submitBtn.disabled = true;
            fetch('/InstructorPanel?handler=CancelReservation', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: `id=${encodeURIComponent(reservationId)}&__RequestVerificationToken=${encodeURIComponent(token)}`
            })
            .then(response => response.json())
            .then(data => {
                if (submitBtn) submitBtn.disabled = false;
                if (data.success) {
                    // Modalı kapat
                    var modal = document.getElementById('cancelReservationModal');
                    if (window.bootstrap && modal) {
                        var bsModal = window.bootstrap.Modal.getInstance(modal);
                        if (bsModal) bsModal.hide();
                    } else if (modal) {
                        $(modal).modal('hide');
                    }
                    showToast(data.message || 'Reservation cancelled!', 'success');
                    // Tabloyu güncelle (veya sayfayı yenile)
                    setTimeout(() => window.location.reload(), 1000);
                } else {
                    showToast(data.message || 'Error cancelling reservation', 'danger');
                }
            })
            .catch(err => {
                if (submitBtn) submitBtn.disabled = false;
                showToast('Error cancelling reservation', 'danger');
            });
        });
    }
});

function showToast(message, type) {
    // Basit Bootstrap toast
    var toast = document.createElement('div');
    toast.className = 'toast align-items-center text-bg-' + (type === 'success' ? 'success' : 'danger') + ' border-0 show position-fixed top-0 end-0 m-3';
    toast.style.zIndex = 2000;
    toast.innerHTML = '<div class="d-flex"><div class="toast-body">' + message + '</div><button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button></div>';
    document.body.appendChild(toast);
    if (window.bootstrap) {
        var bsToast = new window.bootstrap.Toast(toast, { delay: 4000 });
        bsToast.show();
        toast.addEventListener('hidden.bs.toast', function () { toast.remove(); });
    } else {
        setTimeout(() => toast.remove(), 4000);
    }
} 