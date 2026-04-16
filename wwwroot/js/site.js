const dropZone = document.getElementById('dropZone');
const fileInput = document.getElementById('fileInput');
const uploadBtn = document.getElementById('uploadBtn');
const fileName = document.getElementById('fileName');
const uploadForm = document.getElementById('uploadForm');

if (dropZone && fileInput && uploadBtn) {

    // Klick på knappen öppnar filväljaren
    uploadBtn.addEventListener('click', (e) => {
        e.stopPropagation();
        fileInput.click();
    });

    // Visa filnamn och skicka formuläret när fil valts
    fileInput.addEventListener('change', () => {
        if (fileInput.files.length > 0) {
            fileName.textContent = fileInput.files[0].name;
            uploadForm.submit();
        }
    });

    // Drag and drop
    dropZone.addEventListener('dragover', (e) => {
        e.preventDefault();
        dropZone.classList.add('drag-over');
    });

    dropZone.addEventListener('dragleave', () => {
        dropZone.classList.remove('drag-over');
    });

    dropZone.addEventListener('drop', (e) => {
        e.preventDefault();
        dropZone.classList.remove('drag-over');
        const files = e.dataTransfer.files;
        if (files.length > 0) {
            fileInput.files = files;
            fileName.textContent = files[0].name;
            uploadForm.submit();
        }
    });
}


// Configure - ladda produktdetalj
function loadDetail(variantId, item) {
    // Markera aktiv produkt
    document.querySelectorAll('.product-item').forEach(i => i.classList.remove('active'));
    item.classList.add('active');

    // Hämta detaljvy via AJAX
    fetch(`/Configure/Detail?variantId=${encodeURIComponent(variantId)}`)
        .then(res => res.text())
        .then(html => {
            document.getElementById('detailPanel').innerHTML = html;
        });
}

// Configure - filter och checkbox-logik
const brandFilter = document.getElementById('brandFilter');
const categoryFilter = document.getElementById('categoryFilter');
const activeTags = document.getElementById('activeTags');
const selectedCountEl = document.getElementById('selectedCount');
const productItems = document.querySelectorAll('.product-item');

// Aktiva filter
const activeFilters = { brands: [], categories: [] };

function applyFilters() {
    const hasFilter = activeFilters.brands.length > 0 || activeFilters.categories.length > 0;

    let selectedCount = 0;

    productItems.forEach(item => {
        const itemBrand = item.dataset.brand || '';
        const itemCategory = item.dataset.category || '';
        const checkbox = item.querySelector('.product-checkbox');

        const brandMatch = activeFilters.brands.length === 0 || activeFilters.brands.includes(itemBrand);
        const categoryMatch = activeFilters.categories.length === 0 || activeFilters.categories.includes(itemCategory);
        const visible = brandMatch && categoryMatch;

        item.style.display = visible ? '' : 'none';

        if (!hasFilter) {
            checkbox.checked = true;
        } else {
            checkbox.checked = visible;
        }

        if (checkbox.checked) selectedCount++;
    });

    if (selectedCountEl) selectedCountEl.textContent = selectedCount;
}

function renderTags() {
    if (!activeTags) return;
    activeTags.innerHTML = '';

    activeFilters.brands.forEach(brand => {
        addTag(brand, () => {
            activeFilters.brands = activeFilters.brands.filter(b => b !== brand);
            renderTags();
            applyFilters();
        });
    });

    activeFilters.categories.forEach(category => {
        addTag(category, () => {
            activeFilters.categories = activeFilters.categories.filter(c => c !== category);
            renderTags();
            applyFilters();
        });
    });
}

function addTag(label, onRemove) {
    const tag = document.createElement('div');
    tag.className = 'filter-tag';
    tag.innerHTML = `<span>${label}</span><button>×</button>`;
    tag.querySelector('button').addEventListener('click', onRemove);
    activeTags.appendChild(tag);
}

if (brandFilter) {
    brandFilter.addEventListener('change', () => {
        const val = brandFilter.value;
        if (val && !activeFilters.brands.includes(val)) {
            activeFilters.brands.push(val);
            renderTags();
            applyFilters();
        }
        brandFilter.value = '';
    });
}

if (categoryFilter) {
    categoryFilter.addEventListener('change', () => {
        const val = categoryFilter.value;
        if (val && !activeFilters.categories.includes(val)) {
            activeFilters.categories.push(val);
            renderTags();
            applyFilters();
        }
        categoryFilter.value = '';
    });
}

// Sortera produktlista efter datakvalitet
function sortByQuality(quality) {
    const list = document.getElementById('productList');
    if (!list) return;

    const items = Array.from(list.querySelectorAll('.product-item'));

    items.sort((a, b) => {
        const aMatch = a.dataset.quality?.toLowerCase() === quality ? -1 : 1;
        const bMatch = b.dataset.quality?.toLowerCase() === quality ? -1 : 1;
        return aMatch - bMatch;
    });

    items.forEach(item => list.appendChild(item));
}

// Checkbox-räknare
function updateSelectedCount() {
    const count = document.querySelectorAll('.product-checkbox:checked').length;
    if (selectedCountEl) selectedCountEl.textContent = count;
}

document.querySelectorAll('.product-checkbox').forEach(checkbox => {
    checkbox.addEventListener('change', updateSelectedCount);
});