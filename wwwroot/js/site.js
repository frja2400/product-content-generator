// Upload - drag and drop
const dropZone = document.getElementById('dropZone');
const fileInput = document.getElementById('fileInput');
const uploadBtn = document.getElementById('uploadBtn');
const fileName = document.getElementById('fileName');
const uploadForm = document.getElementById('uploadForm');

if (dropZone && fileInput && uploadBtn) {

    uploadBtn.addEventListener('click', (e) => {
        e.stopPropagation();
        fileInput.click();
    });

    fileInput.addEventListener('change', () => {
        if (fileInput.files.length > 0) {
            fileName.textContent = fileInput.files[0].name;
            uploadForm.submit();
        }
    });

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
    document.querySelectorAll('.product-item').forEach(i => i.classList.remove('active'));
    item.classList.add('active');

    fetch(`/Configure/Detail?variantId=${encodeURIComponent(variantId)}`)
        .then(res => res.text())
        .then(html => {
            document.getElementById('detailPanel').innerHTML = html;
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

// Configure - filter, checkbox och sortering
document.addEventListener('DOMContentLoaded', () => {
    const brandFilter = document.getElementById('brandFilter');
    const categoryFilter = document.getElementById('categoryFilter');
    const activeTags = document.getElementById('activeTags');
    const selectedCountEl = document.getElementById('selectedCount');
    const productItems = document.querySelectorAll('.product-item');

    if (!brandFilter && !categoryFilter) return;

    const activeFilters = { brands: [], categories: [] };

    function applyFilters() {
        const categoryKey = categoryFilter?.dataset.categoryKey || 'category0';
        const hasFilter = activeFilters.brands.length > 0 || activeFilters.categories.length > 0;

        let selectedCount = 0;

        productItems.forEach(item => {
            const itemBrand = item.dataset.brand || '';
            const itemCategory = item.dataset[categoryKey] || '';
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

        updateCategoryDropdown();
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

    function updateCategoryDropdown() {
        if (!categoryFilter) return;

        const categoryKey = categoryFilter.dataset.categoryKey || 'category0';
        const currentValue = categoryFilter.value;

        // Hitta vilka kategorier som finns bland synliga produkter
        const visibleCategories = new Set();
        productItems.forEach(item => {
            if (item.style.display !== 'none') {
                const cat = item.dataset[categoryKey];
                if (cat) visibleCategories.add(cat);
            }
        });

        // Uppdatera dropdown-alternativen
        const options = categoryFilter.querySelectorAll('option:not([value=""])');
        options.forEach(option => {
            option.style.display = visibleCategories.has(option.value) ? '' : 'none';
        });

        // Återställ värdet om det inte längre är synligt
        if (!visibleCategories.has(currentValue)) {
            categoryFilter.value = '';
        }
    }

    brandFilter.addEventListener('change', () => {
        const val = brandFilter.value;
        if (val && !activeFilters.brands.includes(val)) {
            activeFilters.brands.push(val);
            renderTags();
            applyFilters();
        }
        brandFilter.value = '';
    });

    categoryFilter.addEventListener('change', () => {
        const val = categoryFilter.value;
        if (val && !activeFilters.categories.includes(val)) {
            activeFilters.categories.push(val);
            renderTags();
            applyFilters();
        }
        categoryFilter.value = '';
    });

    // Checkbox-räknare
    productItems.forEach(checkbox => {
        checkbox.querySelector('.product-checkbox')?.addEventListener('change', () => {
            const count = document.querySelectorAll('.product-checkbox:checked').length;
            if (selectedCountEl) selectedCountEl.textContent = count;
        });
    });

    const cancelBtn = document.getElementById('cancelBtn');

    if (cancelBtn) {
        cancelBtn.addEventListener('click', () => {
            if (confirm('Are you sure you want to cancel? All imported products will be lost.')) {
                window.location.href = '/Upload';
            }
        });
    }
});