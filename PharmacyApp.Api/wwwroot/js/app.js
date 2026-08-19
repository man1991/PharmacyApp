/**
 * ABC Pharmacy - Medicine Tracker (client-side)
 *
 * Plain, dependency-free JavaScript SPA that talks to the ASP.NET Core Web API
 * under /api. Kept framework-free on purpose so the assessment solution has
 * zero build-tooling requirements - open index.html served by the API and it works.
 */

// Base path for the API. Same-origin because the API also serves this SPA from wwwroot.
const API_BASE = "/api";

// ----------------------------------------------------------------------------
// DOM references
// ----------------------------------------------------------------------------
const els = {
  notification: document.getElementById("notification"),

  searchInput: document.getElementById("searchInput"),
  searchBtn: document.getElementById("searchBtn"),
  clearSearchBtn: document.getElementById("clearSearchBtn"),

  medicineTableBody: document.getElementById("medicineTableBody"),
  emptyState: document.getElementById("emptyState"),
  loadingState: document.getElementById("loadingState"),

  salesTableBody: document.getElementById("salesTableBody"),
  salesEmptyState: document.getElementById("salesEmptyState"),

  openAddModalBtn: document.getElementById("openAddModalBtn"),
  addModal: document.getElementById("addModal"),
  addMedicineForm: document.getElementById("addMedicineForm"),
  cancelAddBtn: document.getElementById("cancelAddBtn"),

  sellModal: document.getElementById("sellModal"),
  sellMedicineForm: document.getElementById("sellMedicineForm"),
  sellMedicineId: document.getElementById("sellMedicineId"),
  sellModalMedicineName: document.getElementById("sellModalMedicineName"),
  sellQuantity: document.getElementById("sellQuantity"),
  cancelSellBtn: document.getElementById("cancelSellBtn"),
};

// ----------------------------------------------------------------------------
// Generic API helper - centralizes fetch + user-friendly error handling
// ----------------------------------------------------------------------------

/**
 * Wraps fetch() so every call point gets consistent, friendly error handling
 * instead of duplicating try/catch + status-code checks everywhere.
 */
async function apiRequest(path, options = {}) {
  let response;
  try {
    response = await fetch(`${API_BASE}${path}`, {
      headers: { "Content-Type": "application/json" },
      ...options,
    });
  } catch (networkError) {
    // fetch() itself throws only on network-level failures (server unreachable, offline, etc.)
    throw new Error("Could not reach the server. Please check your connection and try again.");
  }

  // No content responses (e.g. DELETE -> 204) have nothing to parse.
  if (response.status === 204) {
    return null;
  }

  let body = null;
  try {
    body = await response.json();
  } catch {
    // Non-JSON body is fine for a success response with no payload; only a problem if !ok.
  }

  if (!response.ok) {
    // The API's global exception middleware returns { success:false, error: "..." }.
    // ASP.NET Core's built-in model-validation errors return { errors: { field: [msgs] } }.
    const friendlyMessage =
      body?.error ||
      (body?.errors && Object.values(body.errors).flat().join(" ")) ||
      `Request failed (HTTP ${response.status}). Please try again.`;
    throw new Error(friendlyMessage);
  }

  return body;
}

// ----------------------------------------------------------------------------
// Notifications
// ----------------------------------------------------------------------------
let notificationTimer = null;

function showNotification(message, type = "success") {
  els.notification.textContent = message;
  els.notification.className = `notification ${type}`;
  clearTimeout(notificationTimer);
  notificationTimer = setTimeout(() => els.notification.classList.add("hidden"), 5000);
}

// ----------------------------------------------------------------------------
// Medicine grid rendering
// ----------------------------------------------------------------------------

function formatDate(isoString) {
  const date = new Date(isoString);
  return date.toLocaleDateString(undefined, { year: "numeric", month: "short", day: "numeric" });
}

function formatCurrency(amount) {
  return `$${Number(amount).toFixed(2)}`;
}

function isNearExpiry(isoDate) {
  const msPerDay = 1000 * 60 * 60 * 24;
  const daysUntilExpiry = (new Date(isoDate) - new Date()) / msPerDay;
  return daysUntilExpiry < 30;
}

function isLowStock(quantity) {
  return quantity < 10;
}

function renderMedicineRow(medicine) {
  const tr = document.createElement("tr");
  const rowClasses = [];
  if (isNearExpiry(medicine.expiryDate)) rowClasses.push("row-expiry");
  if (isLowStock(medicine.quantity)) rowClasses.push("row-lowstock");
  tr.className = rowClasses.join(" ");

  tr.innerHTML = `
    <td>${escapeHtml(medicine.fullName)}</td>
    <td>${escapeHtml(medicine.brand)}</td>
    <td>${formatDate(medicine.expiryDate)}</td>
    <td>${medicine.quantity}</td>
    <td>${formatCurrency(medicine.price)}</td>
    <td>
      <button class="btn btn-secondary btn-small" data-action="sell" data-id="${medicine.id}">Sell</button>
      <button class="btn btn-link btn-small" data-action="delete" data-id="${medicine.id}">Delete</button>
    </td>
  `;
  return tr;
}

/** Minimal HTML-escaping so medicine names/brands can't break the markup. */
function escapeHtml(value) {
  const div = document.createElement("div");
  div.textContent = value ?? "";
  return div.innerHTML;
}

async function loadMedicines(searchTerm = "") {
  els.loadingState.classList.remove("hidden");
  els.emptyState.classList.add("hidden");
  els.medicineTableBody.innerHTML = "";

  try {
    const query = searchTerm ? `?search=${encodeURIComponent(searchTerm)}` : "";
    const medicines = await apiRequest(`/medicines${query}`);

    if (!medicines || medicines.length === 0) {
      els.emptyState.classList.remove("hidden");
      return;
    }

    const fragment = document.createDocumentFragment();
    medicines.forEach((m) => fragment.appendChild(renderMedicineRow(m)));
    els.medicineTableBody.appendChild(fragment);
  } catch (err) {
    showNotification(err.message, "error");
  } finally {
    els.loadingState.classList.add("hidden");
  }
}

// ----------------------------------------------------------------------------
// Sales history rendering
// ----------------------------------------------------------------------------

function renderSaleRow(sale) {
  const tr = document.createElement("tr");
  tr.innerHTML = `
    <td>${formatDate(sale.saleDate)}</td>
    <td>${escapeHtml(sale.medicineName)}</td>
    <td>${sale.quantitySold}</td>
    <td>${formatCurrency(sale.unitPrice)}</td>
    <td>${formatCurrency(sale.totalAmount)}</td>
  `;
  return tr;
}

async function loadSales() {
  try {
    const sales = await apiRequest("/sales");
    els.salesTableBody.innerHTML = "";

    if (!sales || sales.length === 0) {
      els.salesEmptyState.classList.remove("hidden");
      return;
    }
    els.salesEmptyState.classList.add("hidden");

    const fragment = document.createDocumentFragment();
    sales.slice(0, 20).forEach((s) => fragment.appendChild(renderSaleRow(s)));
    els.salesTableBody.appendChild(fragment);
  } catch (err) {
    showNotification(err.message, "error");
  }
}

// ----------------------------------------------------------------------------
// Add Medicine modal
// ----------------------------------------------------------------------------

function openAddModal() {
  els.addMedicineForm.reset();
  clearFieldErrors(els.addMedicineForm);
  els.addModal.classList.remove("hidden");
}

function closeAddModal() {
  els.addModal.classList.add("hidden");
}

function clearFieldErrors(form) {
  form.querySelectorAll(".field-error").forEach((el) => (el.textContent = ""));
}

/** Basic client-side validation mirroring the server's rules, for instant feedback. */
function validateAddForm(data) {
  const errors = {};
  if (!data.fullName.trim()) errors.fullName = "Full name is required.";
  if (!data.brand.trim()) errors.brand = "Brand is required.";
  if (!data.expiryDate) errors.expiryDate = "Expiry date is required.";
  if (data.quantity === "" || Number(data.quantity) < 0) errors.quantity = "Quantity must be 0 or more.";
  if (data.price === "" || Number(data.price) < 0) errors.price = "Price must be 0 or more.";
  return errors;
}

function showFieldErrors(form, errors) {
  Object.entries(errors).forEach(([field, message]) => {
    const el = form.querySelector(`[data-error-for="${field}"]`);
    if (el) el.textContent = message;
  });
}

els.openAddModalBtn.addEventListener("click", openAddModal);
els.cancelAddBtn.addEventListener("click", closeAddModal);

els.addMedicineForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  clearFieldErrors(els.addMedicineForm);

  const formData = new FormData(els.addMedicineForm);
  const payload = {
    fullName: formData.get("fullName"),
    brand: formData.get("brand"),
    expiryDate: formData.get("expiryDate"),
    quantity: formData.get("quantity"),
    price: formData.get("price"),
    notes: formData.get("notes"),
  };

  const clientErrors = validateAddForm(payload);
  if (Object.keys(clientErrors).length > 0) {
    showFieldErrors(els.addMedicineForm, clientErrors);
    return;
  }

  try {
    await apiRequest("/medicines", {
      method: "POST",
      body: JSON.stringify({
        ...payload,
        quantity: Number(payload.quantity),
        price: Number(payload.price),
      }),
    });
    showNotification(`"${payload.fullName}" was added successfully.`, "success");
    closeAddModal();
    await loadMedicines(els.searchInput.value.trim());
  } catch (err) {
    // Server-side validation / business-rule failures surface here as a friendly message.
    showNotification(err.message, "error");
  }
});

// ----------------------------------------------------------------------------
// Sell Medicine modal
// ----------------------------------------------------------------------------

function openSellModal(id, name) {
  els.sellMedicineId.value = id;
  els.sellModalMedicineName.textContent = name;
  els.sellQuantity.value = 1;
  clearFieldErrors(els.sellMedicineForm);
  els.sellModal.classList.remove("hidden");
}

function closeSellModal() {
  els.sellModal.classList.add("hidden");
}

els.cancelSellBtn.addEventListener("click", closeSellModal);

els.sellMedicineForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  clearFieldErrors(els.sellMedicineForm);

  const quantity = Number(els.sellQuantity.value);
  if (!quantity || quantity < 1) {
    showFieldErrors(els.sellMedicineForm, { sellQuantity: "Enter a quantity of at least 1." });
    return;
  }

  try {
    await apiRequest("/sales", {
      method: "POST",
      body: JSON.stringify({
        medicineId: els.sellMedicineId.value,
        quantitySold: quantity,
      }),
    });
    showNotification("Sale recorded successfully.", "success");
    closeSellModal();
    await Promise.all([loadMedicines(els.searchInput.value.trim()), loadSales()]);
  } catch (err) {
    // e.g. "Cannot sell 15 unit(s) of 'X' - only 8 in stock."
    showNotification(err.message, "error");
  }
});

// ----------------------------------------------------------------------------
// Row actions (event delegation - the grid re-renders often, so we attach once)
// ----------------------------------------------------------------------------

els.medicineTableBody.addEventListener("click", async (event) => {
  const button = event.target.closest("button[data-action]");
  if (!button) return;

  const id = button.dataset.id;
  const action = button.dataset.action;
  const medicineName = button.closest("tr").querySelector("td").textContent;

  if (action === "sell") {
    openSellModal(id, medicineName);
    return;
  }

  if (action === "delete") {
    const confirmed = window.confirm(`Remove "${medicineName}" from the system? This cannot be undone.`);
    if (!confirmed) return;

    try {
      await apiRequest(`/medicines/${id}`, { method: "DELETE" });
      showNotification(`"${medicineName}" was removed.`, "success");
      await loadMedicines(els.searchInput.value.trim());
    } catch (err) {
      showNotification(err.message, "error");
    }
  }
});

// ----------------------------------------------------------------------------
// Search
// ----------------------------------------------------------------------------

els.searchBtn.addEventListener("click", () => loadMedicines(els.searchInput.value.trim()));
els.searchInput.addEventListener("keyup", (event) => {
  if (event.key === "Enter") loadMedicines(els.searchInput.value.trim());
});
els.clearSearchBtn.addEventListener("click", () => {
  els.searchInput.value = "";
  loadMedicines();
});

// ----------------------------------------------------------------------------
// Initial load
// ----------------------------------------------------------------------------

loadMedicines();
loadSales();
