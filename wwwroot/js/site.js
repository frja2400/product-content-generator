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

// Inaktivera Run sample om inga produkter är valda
function updateSampleButton() {
    const sampleBtn = document.getElementById('sampleBtn');
    if (!sampleBtn) return;

    const checkedCount = document.querySelectorAll('.product-checkbox:checked').length;
    sampleBtn.disabled = checkedCount === 0;
    sampleBtn.style.opacity = checkedCount === 0 ? '0.4' : '1';
    sampleBtn.style.cursor = checkedCount === 0 ? 'not-allowed' : 'pointer';
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
        updateSampleButton();
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

        const visibleCategories = new Set();
        productItems.forEach(item => {
            if (item.style.display !== 'none') {
                const cat = item.dataset[categoryKey];
                if (cat) visibleCategories.add(cat);
            }
        });

        const options = categoryFilter.querySelectorAll('option:not([value=""])');
        options.forEach(option => {
            option.style.display = visibleCategories.has(option.value) ? '' : 'none';
        });

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
            updateSampleButton();
        });
    });

    // Anropa vid sidladdning
    updateSampleButton();

    // Fyll i valda produkter och visa laddningsindikator vid submit
    document.getElementById('sampleForm')?.addEventListener('submit', () => {
        const container = document.getElementById('selectedProductInputs');
        if (container) {
            container.innerHTML = '';
            document.querySelectorAll('.product-checkbox:checked').forEach(cb => {
                const variantId = cb.closest('.product-item').dataset.variantId;
                const input = document.createElement('input');
                input.type = 'hidden';
                input.name = 'selectedVariantIds';
                input.value = variantId;
                container.appendChild(input);
            });
        }

        const sampleBtn = document.getElementById('sampleBtn');
        if (sampleBtn) {
            sampleBtn.disabled = true;
            sampleBtn.textContent = 'Generating...';
        }
    });
});

// Cancel import
document.querySelectorAll('.cancel-import-btn').forEach(btn => {
    btn.addEventListener('click', () => {
        if (confirm('Are you sure you want to cancel? All progress will be lost.')) {
            window.location.href = '/Upload';
        }
    });
});

// Try again per produkt
document.querySelectorAll('.btn-retry').forEach(btn => {
    btn.addEventListener('click', async () => {
        const variantId = btn.dataset.variantId;
        btn.textContent = 'Generating...';
        btn.disabled = true;

        const response = await fetch(`/Review/RetryGeneration?variantId=${encodeURIComponent(variantId)}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' }
        });

        const result = await response.json();
        const card = btn.closest('.product-card');

        const cardText = card.querySelector('.card-text');
        if (cardText) cardText.innerHTML = result.generatedDescription.replace(/\n/g, '<br>');

        if (result.success) {
            const errorMsg = card.querySelector('.card-error');
            if (errorMsg) errorMsg.remove();
            btn.remove();
        } else {
            btn.textContent = 'Try again';
            btn.disabled = false;
        }
    });
});

// Export - ladda produktdetalj
function loadExportDetail(variantId, item) {
    document.querySelectorAll('.product-item').forEach(i => i.classList.remove('active'));
    item.classList.add('active');

    fetch(`/Export/Detail?variantId=${encodeURIComponent(variantId)}`)
        .then(res => res.text())
        .then(html => {
            document.getElementById('exportDetailPanel').innerHTML = html;
        });
}

// Progress polling
const progressText = document.getElementById('progressText');
const progressBar = document.getElementById('progressBar');

if (progressText && progressBar) {
    const poll = setInterval(async () => {
        const response = await fetch('/Configure/GetProgress');
        const data = await response.json();

        if (data.total > 0) {
            const percent = Math.round((data.completed / data.total) * 100);
            progressText.textContent = `${data.completed} of ${data.total} products generated`;
            progressBar.style.width = `${percent}%`;
        }

        if (data.done) {
            clearInterval(poll);
            window.location.href = '/Export';
        }
    }, 2000);
}

// Run sample again via AJAX
const rerunSampleBtn = document.getElementById('rerunSampleBtn');
if (rerunSampleBtn) {
    rerunSampleBtn.addEventListener('click', async () => {
        const prompt = document.querySelector('.prompt-textarea')?.value;
        const sampleCount = document.getElementById('sampleCountRerun')?.value || 3;

        rerunSampleBtn.disabled = true;
        rerunSampleBtn.textContent = 'Generating...';

        const response = await fetch('/Review/RunSampleAgain', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ prompt, sampleCount: parseInt(sampleCount) })
        });

        const data = await response.json();

        if (data.success) {
            // Rensa alla befintliga produktkort
            const reviewLeft = document.querySelector('.review-left');
            if (reviewLeft) reviewLeft.innerHTML = '';

            // Lägg till nya produktkort
            data.results.forEach(result => {
                const card = document.createElement('div');
                card.className = 'product-card';
                card.setAttribute('data-variant-id', result.variantId);
                card.innerHTML = `
            <div class="product-card-header">
                <div class="product-card-title">
                    <h3 class="card-name">${result.displayName}</h3>
                    <span class="card-variant">${result.variantId}</span>
                </div>
            </div>
            <div class="card-generated">
                <h4 class="card-section-label">Generated description</h4>
                <div class="card-text">${result.generatedDescription.replace(/\n/g, '<br>')}</div>
            </div>
            ${result.generationFailed ? '<p class="card-error">Generation failed – showing original text.</p>' : ''}
        `;
                reviewLeft.appendChild(card);
            });
        }

        rerunSampleBtn.disabled = false;
        rerunSampleBtn.textContent = 'Run sample again';
    });
}