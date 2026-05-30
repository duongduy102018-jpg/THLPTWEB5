(function () {
    const imageInput = document.getElementById('imageInput');
    const previewImage = document.getElementById('previewImage');

    if (imageInput && previewImage) {
        imageInput.addEventListener('change', function (event) {
            const file = event.target.files && event.target.files[0];
            if (!file) return;

            const reader = new FileReader();
            reader.onload = function (e) {
                previewImage.src = e.target.result;
            };
            reader.readAsDataURL(file);
        });
    }
})();
