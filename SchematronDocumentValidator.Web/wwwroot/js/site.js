const input = document.querySelector('#document');
const zone = document.querySelector('#dropzone');
const selected = document.querySelector('#selected-file');
const button = document.querySelector('#validate-button');
const error = document.querySelector('#upload-error');

function updateFile() {
    const file = input.files[0];

    const valid =
        !!file &&
        /\.xml$/i.test(file.name) &&
        file.size > 0 &&
        file.size <= 10 * 1024 * 1024;

    button.disabled = !valid;
    selected.hidden = !valid;
    error.hidden = !file || valid;

    error.textContent =
        'En fazla 10 MB boyutunda, boş olmayan bir XML dosyası seç.';

    if (valid) {
        document.querySelector('#file-name').textContent = file.name;

        document.querySelector('#file-size').textContent =
            (file.size / 1024).toFixed(1) + ' KB · Yüklemeye hazır';
    }
}

input.addEventListener('change', updateFile);

// Dosya sürüklenirken alanı vurgula.
for (const eventName of ['dragenter', 'dragover']) {
    zone.addEventListener(eventName, function (event) {
        event.preventDefault();
        zone.classList.add('dragging');
    });
}

for (const eventName of ['dragleave', 'drop']) {
    zone.addEventListener(eventName, function (event) {
        event.preventDefault();
        zone.classList.remove('dragging');
    });
}

// Bırakılan dosyayı seç.
zone.addEventListener('drop', function (event) {
    if (event.dataTransfer.files.length !== 1) {
        input.value = '';
        updateFile();

        error.hidden = false;
        error.textContent = 'Tek seferde bir XML dosyası seç.';
        return;
    }

    input.files = event.dataTransfer.files;
    updateFile();
});

// Dosya seçimini temizle.
document.querySelector('#remove-file').addEventListener('click', function () {
    input.value = '';
    updateFile();
    input.focus();
});

// Doğrulama sırasında tekrar gönderimi engelle.
document.querySelector('#upload-form').addEventListener('submit', function () {
    button.disabled = true;
    button.textContent = 'Kontrol ediliyor…';
    document.querySelector('#progress').hidden = false;
});

// Geri tuşuyla dönüldüğünde ekranı yeniden hazırla.
window.addEventListener('pageshow', function () {
    updateFile();

    button.innerHTML =
        'Doğrulamayı Başlat <span aria-hidden="true">→</span>';

    document.querySelector('#progress').hidden = true;
});

// Hata mesajlarında ara.
const search = document.querySelector('#error-search');

search?.addEventListener('input', function () {
    const query = search.value.toLocaleLowerCase('tr');
    let count = 0;

    document.querySelectorAll('.violation').forEach(function (row) {
        const message = row.textContent.toLocaleLowerCase('tr');

        row.hidden = !message.includes(query);

        if (!row.hidden) {
            count++;
        }
    });

    document.querySelector('#visible-count').textContent =
        count + ' sonuç';

    document.querySelector('#no-matches').hidden = count !== 0;
});