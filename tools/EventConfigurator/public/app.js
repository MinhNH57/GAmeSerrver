let eventsTimeConfigData = null;

const PREDEFINED_EVENT_KEYS = [
    { key: "EventCompose", desc: "Event ghép" },
    { key: "SlotMachine", desc: "Event Ngũ Phúc Lâm Môn" },
    { key: "SlotMachine_ExchangeTicket", desc: "Event Chuyển Xu Nhận Vé (ngũ phúc)" },
    { key: "BaoThach_ExchangeTicket", desc: "Event Chuyển Xu Nhận Bảo Thạch" },
    { key: "DayRecharge", desc: "Event Ưu Đãi Nạp Đơn" },
    { key: "FlowerTower", desc: "Event Thiên Tầng Tháp" },
    { key: "AccumulateRecharge", desc: "Event Tích Lũy Chuyển Xu" },
    { key: "RaceTop", desc: "Event đua top" },
    { key: "UuDaiDoanKet", desc: "Event ưu đãi đoàn kết" },
    { key: "ComebackEvent", desc: "Event Hảo Hữu Tương Phùng" },
    { key: "SideTask", desc: "Event Nhiệm vụ trưởng thàn" },
    { key: "GiftExchange", desc: "Event đổi quà" },
    { key: "GiftExchangePlayer", desc: "Event đổi quà người chơi" },
    { key: "NewbieGiftcode", desc: "Event giftcode tân thủ" },
    { key: "CheckInEvent", desc: "Event Check-in" },
    { key: "RechargeEvent", desc: "Event Tích Lũy Nạp Liên Tiếp" },
    { key: "DaoTacDisplay", desc: "Event đạo tặc (hiển thị)" },
    { key: "FishingEvent", desc: "Event câu cá" },
    { key: "OfferingEvent", desc: "Event dâng hương" },
    { key: "QuestEvent", desc: "Event nhiệm vụ" },
    { key: "SeashellEvent", desc: "Event Bách Bảo Rương" },
    { key: "SeashellEvent_FreeTicket", desc: "Event Chuyển Xu Nhận Vé Bách Bảo Rương" },
    { key: "MergeServerReward", desc: "Event Quà liên thông" },
    { key: "PetRace", desc: "Giao diện Đua Thú" },
    { key: "UuDaiDisplay", desc: "Giao diện Ưu Đãi" },
    { key: "UuDaiDisplay_NewServer", desc: "Giao diện Ưu Đãi Server Mới" }
];

const FLOWER_EVENT_TYPES = [
    { id: 10008, name: "Tham gia Bạch Hổ Đường" },
    { id: 10004, name: "Chúc Phúc" },
    { id: 10007, name: "Tham gia Tiêu Dao Cốc" },
    { id: 10003, name: "Nhiệm vụ Bao Vạn Đồng/Thương Hội" },
    { id: 10002, name: "Sử dụng vật phẩm/Sách EXP" },
    { id: 11, name: "Tiêu diệt Boss (MonsterDead)" },
    { id: 10040, name: "Nạp KNB (Recharge)" },
    { id: 10009, name: "Khiêu chiến Du Long" },
    { id: 10010, name: "Bí Cảnh" },
    { id: 10024, name: "Tham gia Đua thú" },
    { id: 10023, name: "Cường hóa Chân Nguyên" },
    { id: 10021, name: "Hạng Tống Kim" },
    { id: 17, name: "Sát thương Boss (BloodChanged)" },
    { id: 10022, name: "Hạ gục người trong Tống Kim" },
    { id: 10036, name: "Ghép vật phẩm Event" },
    { id: 10019, name: "Trang bị Phi Phong" },
    { id: 10016, name: "Hạ gục người trong Bạch Hổ Đường" },
    { id: 10014, name: "Nhận thưởng thi đấu môn phái" },
    { id: 10041, name: "Quay Tân Niên Bí Bảo (Spin_Lucky)" }
];

let scriptItemsFlowerTower = [];
let randomboxesFlowerTower = [];

let scriptItemsGeneral = [];
let randomboxesGeneral = [];

let luckySpinData = null;
let serverMergeData = null;
let battlePassData = null;
let currentUuDaiType = 'normal';

document.addEventListener('DOMContentLoaded', () => {
    loadGlobalTimes();
    loadComposeTimes();
    loadComposeXml();
    loadSlotMachine();
    loadRechargeXml(); // Only RechargeEvent_OldServer.xml
    loadCheckIn();
    loadSeashell();
    loadFlowerTower();
    loadExchangeTicket();
    loadRechargeManagement(); // Load everything for the new tab
    loadUuDaiDisplay();
    loadLuckySpin();
    loadServerMerge();
    loadBattlePass();

    document.addEventListener('wheel', event => {
        const target = event.target;
        if (target instanceof HTMLElement && target.matches('input[type="number"]') && document.activeElement === target) {
            event.preventDefault();
        }
    }, { passive: false });
});

function switchTab(tabId) {
    const sections = document.querySelectorAll('.tab-section');
    const links = document.querySelectorAll('.sidebar a');

    // Hide and animate out
    sections.forEach(el => {
        el.classList.add('d-none');
        el.style.opacity = '0';
    });

    links.forEach(el => el.classList.remove('active'));

    // Show selected section
    const target = document.getElementById(`${tabId}-section`);
    if (target) {
        target.classList.remove('d-none');
        // Trigger reflow for animation
        void target.offsetWidth;
        target.style.opacity = '1';
    }

    // Set active link
    const activeLink = document.querySelector(`.sidebar a[href="#${tabId}"]`);
    if (activeLink) activeLink.classList.add('active');

    // Optional dynamic loading
    if (tabId === 'recharge-management') loadRechargeManagement();
    if (tabId === 'uudai') loadUuDaiDisplay();
    if (tabId === 'recharge') loadRechargeOldServer();
    if (tabId === 'luckyspin') loadLuckySpin();
    if (tabId === 'servermerge') loadServerMerge();
    if (tabId === 'battlepass') loadBattlePass();
}

/**
 * Get all searchable text from an element, including:
 * - Visible text (labels, spans, etc.)
 * - Input/textarea values
 * - Selected option text from selects
 */
function getSearchableText(el) {
    let parts = [];

    // Collect visible text from non-input elements
    el.childNodes.forEach(node => {
        if (node.nodeType === Node.TEXT_NODE) {
            const t = node.textContent.trim();
            if (t) parts.push(t);
        }
    });

    // Collect all input values
    el.querySelectorAll('input, textarea').forEach(input => {
        if (input.value) parts.push(input.value);
    });

    // Collect selected option text from selects
    el.querySelectorAll('select').forEach(sel => {
        if (sel.selectedIndex >= 0) {
            parts.push(sel.options[sel.selectedIndex].text);
        }
    });

    // Collect text from spans, labels, badges, etc.
    el.querySelectorAll('span, label, h5, h6, .badge, .input-group-text, .card-header, th, td').forEach(node => {
        // Only get direct text, skip if it contains inputs (already handled)
        node.childNodes.forEach(child => {
            if (child.nodeType === Node.TEXT_NODE) {
                const t = child.textContent.trim();
                if (t) parts.push(t);
            }
        });
    });

    return parts.join(' ').toLowerCase();
}

/**
 * Filter items within a section based on search query
 * Searches both visible text and input/select values
 */
function filterSection(sectionId, query) {
    const section = document.getElementById(sectionId);
    if (!section) return;

    const q = query.toLowerCase().trim();

    // If query is empty, show everything
    if (!q) {
        section.querySelectorAll('tbody tr').forEach(row => row.style.display = '');
        section.querySelectorAll('.list-group-item').forEach(item => item.style.display = '');
        section.querySelectorAll('.card').forEach(card => {
            if (card.closest('.tab-section') === card) return; // skip section itself
            card.style.display = '';
        });
        return;
    }

    // Search in Table Rows
    section.querySelectorAll('tbody tr').forEach(row => {
        const text = getSearchableText(row);
        row.style.display = text.includes(q) ? '' : 'none';
    });

    // Search in List Groups (e.g., Script Items)
    section.querySelectorAll('.list-group-item').forEach(item => {
        const text = getSearchableText(item);
        item.style.display = text.includes(q) ? '' : 'none';
    });

    // Search in Cards (for complex sections like server configs, floor tasks)
    section.querySelectorAll('.card').forEach(card => {
        // Don't hide the parent section itself or top-level tab-section
        if (card.id === sectionId || card.classList.contains('tab-section')) return;
        // Don't hide cards that are containers for sub-tables (check if inside another filterable card)
        if (card.closest('.card') && card.closest('.card') !== card && card.closest('.card').id !== sectionId) return;

        const text = getSearchableText(card);
        card.style.display = text.includes(q) ? '' : 'none';
    });
}

// Helper to convert yyyy-MM-dd HH:mm:ss to yyyy-MM-ddTHH:mm:ss for input
function formatToInput(str) {
    if (!str) return '';
    return str.replace(' ', 'T');
}

// Helper to convert yyyy-MM-ddTHH:mm:ss back to yyyy-MM-dd HH:mm:ss for XML
function formatToXml(str) {
    if (!str) return '';
    return str.replace('T', ' ');
}

function showAlert(message, type = 'success') {
    if (type === 'success' || type === 'info') {
        const toastEl = document.getElementById('saveToast');
        if (toastEl) {
            document.getElementById('toast-message').innerHTML = `<i class="bi bi-${type === 'success' ? 'check-circle-fill' : 'info-circle-fill'} me-2"></i> ${message}`;
            toastEl.className = `toast align-items-center text-bg-${type} border-0`;
            const toast = new bootstrap.Toast(toastEl, { delay: 3500 });
            toast.show();
        }
    } else {
        const alertHtml = `
            <div class="alert alert-${type} alert-dismissible fade show" role="alert">
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            </div>
        `;
        document.getElementById('alert-container').innerHTML = alertHtml;
        window.scrollTo({ top: 0, behavior: 'smooth' });
        // Auto clear after 7s
        setTimeout(() => {
            document.getElementById('alert-container').innerHTML = '';
        }, 7000);
    }
}

// ----------------------------------------------------
// GLOBAL TIMES (EventsTimeConfig.xml)
// ----------------------------------------------------

async function loadGlobalTimes() {
    try {
        const response = await fetch('/api/events-time-config');
        if (!response.ok) throw new Error('Failed to fetch data');

        eventsTimeConfigData = await response.json();
        renderGlobalTimesForm(eventsTimeConfigData);
    } catch (error) {
        console.error(error);
        showAlert('Lỗi khi tải dữ liệu EventsTimeConfig.xml. Server có đang chạy không?', 'danger');
        document.getElementById('global-times-form').innerHTML = '<p class="text-danger">Không thể tải dữ liệu.</p>';
    }
}

function renderGlobalTimesForm(data) {
    const container = document.getElementById('global-times-form');
    container.innerHTML = '';

    // Create datalist for event keys
    let datalistHtml = '<datalist id="event-keys-list">';
    PREDEFINED_EVENT_KEYS.forEach(item => {
        datalistHtml += `<option value="${item.key}">${item.desc}</option>`;
    });
    datalistHtml += '</datalist>';
    container.insertAdjacentHTML('beforeend', datalistHtml);

    if (!data.EventsTimeConfig || !data.EventsTimeConfig.Server) {
        // Init empty if blank
        data.EventsTimeConfig = { Server: [] };
    }

    const servers = data.EventsTimeConfig.Server;

    servers.forEach((serverNode, serverIndex) => {
        const sAttr = serverNode.$ || {};
        const serverId = sAttr.Id || '';

        let headerLabel = `Cụm Server ID: ${serverId}`;
        if (serverId === '-1') headerLabel += ' (Server Chung - Mặc định)';
        if (serverId === '999') headerLabel += ' (Server Test)';
        if (serverId === '901') headerLabel += ' (Server Local)';

        const serverCard = document.createElement('div');
        serverCard.className = 'card mb-4 border-secondary';

        let html = `
            <div class="card-header bg-secondary text-white d-flex justify-content-between align-items-center">
                <span class="fw-bold">${headerLabel}</span>
                <button class="btn btn-sm btn-danger" onclick="removeServer(${serverIndex})"><i class="bi bi-trash"></i> Xóa Cụm Server</button>
            </div>
            <div class="card-body">
                <div class="row mb-3">
                    <div class="col-md-2">
                        <label class="form-label">Server ID</label>
                        <input type="text" class="form-control s-attr" data-server-idx="${serverIndex}" data-attr="Id" value="${sAttr.Id || ''}">
                    </div>
                    <div class="col-md-3">
                        <label class="form-label">StartDateEvtBtn</label>
                        <input type="datetime-local" step="1" class="form-control s-attr" data-server-idx="${serverIndex}" data-attr="StartDateEvtBtn" value="${formatToInput(sAttr.StartDateEvtBtn || '')}">
                    </div>
                    <div class="col-md-3">
                        <label class="form-label">EndDateEvtBtn</label>
                        <input type="datetime-local" step="1" class="form-control s-attr" data-server-idx="${serverIndex}" data-attr="EndDateEvtBtn" value="${formatToInput(sAttr.EndDateEvtBtn || '')}">
                    </div>
                    <div class="col-md-2">
                        <label class="form-label">IsNewServer</label>
                        <select class="form-select s-attr" data-server-idx="${serverIndex}" data-attr="IsNewServer" onchange="toggleNewServerDates(${serverIndex}, this.value)">
                            <option value="true" ${sAttr.IsNewServer === 'true' ? 'selected' : ''}>true</option>
                            <option value="false" ${sAttr.IsNewServer === 'false' ? 'selected' : ''}>false</option>
                        </select>
                    </div>
                </div>
                <!-- Dòng cấu hình thời gian hiển thị nút Event cho Server Mới -->
                <div class="row mb-3" id="newServerDatesRow-${serverIndex}" style="display: ${sAttr.IsNewServer === 'true' ? 'flex' : 'none'};">
                    <div class="col-md-3">
                        <label class="form-label text-primary">StartDateNewServerBtn</label>
                        <input type="datetime-local" step="1" class="form-control s-attr border-primary" data-server-idx="${serverIndex}" data-attr="StartDateNewServerBtn" value="${formatToInput(sAttr.StartDateNewServerBtn || '')}">
                    </div>
                    <div class="col-md-3">
                        <label class="form-label text-primary">EndDateNewServerBtn</label>
                        <input type="datetime-local" step="1" class="form-control s-attr border-primary" data-server-idx="${serverIndex}" data-attr="EndDateNewServerBtn" value="${formatToInput(sAttr.EndDateNewServerBtn || '')}">
                    </div>
                </div>
                
                <div class="d-flex justify-content-between align-items-center mt-4 mb-2">
                    <h5>Danh sách Event con:</h5>
                    <button class="btn btn-sm btn-success" onclick="addEvent(${serverIndex})"><i class="bi bi-plus-circle"></i> Thêm Event Key</button>
                </div>
                <div class="table-responsive">
                    <table class="table table-sm table-bordered">
                        <thead class="table-light">
                            <tr>
                                <th width="20%">Key</th>
                                <th>StartDate</th>
                                <th>EndDate</th>
                                <th width="15%">Trạng Thái</th>
                                <th width="15%">NewServerEvent</th>
                                <th width="5%"></th>
                            </tr>
                        </thead>
                        <tbody>
        `;

        if (serverNode.Event && Array.isArray(serverNode.Event)) {
            serverNode.Event.forEach((evtNode, evtIndex) => {
                const eAttr = evtNode.$ || {};
                html += `
                    <tr>
                        <td><input type="text" class="form-control form-control-sm e-attr fw-bold text-primary" list="event-keys-list" data-server-idx="${serverIndex}" data-evt-idx="${evtIndex}" data-attr="Key" value="${eAttr.Key || ''}"></td>
                        <td><input type="datetime-local" step="1" class="form-control form-control-sm e-attr" data-server-idx="${serverIndex}" data-evt-idx="${evtIndex}" data-attr="StartDate" value="${formatToInput(eAttr.StartDate || '')}"></td>
                        <td><input type="datetime-local" step="1" class="form-control form-control-sm e-attr" data-server-idx="${serverIndex}" data-evt-idx="${evtIndex}" data-attr="EndDate" value="${formatToInput(eAttr.EndDate || '')}"></td>
                        <td>
                            <select class="form-select form-select-sm e-attr" data-server-idx="${serverIndex}" data-evt-idx="${evtIndex}" data-attr="Enable">
                                <option value="true" ${eAttr.Enable === 'true' ? 'selected' : ''}>Bật (true)</option>
                                <option value="false" ${eAttr.Enable === 'false' ? 'selected' : ''}>Tắt (false)</option>
                            </select>
                        </td>
                        <td>
                            <select class="form-select form-select-sm e-attr" data-server-idx="${serverIndex}" data-evt-idx="${evtIndex}" data-attr="NewServerEvent">
                                <option value="true" ${eAttr.NewServerEvent === 'true' ? 'selected' : ''}>true</option>
                                <option value="false" ${eAttr.NewServerEvent === 'false' ? 'selected' : ''}>false</option>
                            </select>
                        </td>
                        <td class="text-center">
                            <button class="btn btn-sm btn-outline-danger" onclick="removeEvent(${serverIndex}, ${evtIndex})"><i class="bi bi-trash"></i></button>
                        </td>
                    </tr>
                `;
            });
        }

        html += `
                        </tbody>
                    </table>
                </div>
            </div>
        `;

        serverCard.innerHTML = html;
        container.appendChild(serverCard);
    });

    // Add Server button at the bottom
    const addServerBtn = document.createElement('div');
    addServerBtn.className = 'text-center mt-3';
    addServerBtn.innerHTML = `<button class="btn btn-lg btn-success" onclick="addServer()"><i class="bi bi-plus-circle"></i> Thêm Cụm Server Mới</button>`;
    container.appendChild(addServerBtn);
}

function toggleNewServerDates(serverIndex, value) {
    const row = document.getElementById(`newServerDatesRow-${serverIndex}`);
    if (row) {
        row.style.display = value === 'true' ? 'flex' : 'none';
    }
}

function gatherGlobalTimesFormData() {
    document.querySelectorAll('.s-attr').forEach(input => {
        const sIdx = input.getAttribute('data-server-idx');
        const attrName = input.getAttribute('data-attr');
        let val = input.value;
        if (input.type === 'datetime-local') val = formatToXml(val);
        eventsTimeConfigData.EventsTimeConfig.Server[sIdx].$[attrName] = val;
    });

    document.querySelectorAll('.e-attr').forEach(input => {
        const sIdx = input.getAttribute('data-server-idx');
        const eIdx = input.getAttribute('data-evt-idx');
        const attrName = input.getAttribute('data-attr');
        let val = input.value;
        if (input.type === 'datetime-local') val = formatToXml(val);
        eventsTimeConfigData.EventsTimeConfig.Server[sIdx].Event[eIdx].$[attrName] = val;
    });

    return eventsTimeConfigData;
}

function addServer() {
    gatherGlobalTimesFormData(); // save current inputs

    if (!eventsTimeConfigData.EventsTimeConfig.Server) {
        eventsTimeConfigData.EventsTimeConfig.Server = [];
    }

    eventsTimeConfigData.EventsTimeConfig.Server.push({
        $: { Id: "New_Server", StartDateEvtBtn: "2026-01-01 00:00:00", EndDateEvtBtn: "2026-12-31 23:59:59", IsNewServer: "false", StartDateNewServerBtn: "2026-01-01 00:00:00", EndDateNewServerBtn: "2026-12-31 23:59:59" },
        Event: []
    });

    renderGlobalTimesForm(eventsTimeConfigData);
}

function removeServer(sIdx) {
    if (confirm('Bạn có chắc chắn muốn xóa Cụm Server này?')) {
        eventsTimeConfigData.EventsTimeConfig.Server.splice(sIdx, 1);
        renderGlobalTimesForm(eventsTimeConfigData);
    }
}

function addEvent(sIdx) {
    gatherGlobalTimesFormData(); // save current inputs

    if (!eventsTimeConfigData.EventsTimeConfig.Server[sIdx].Event) {
        eventsTimeConfigData.EventsTimeConfig.Server[sIdx].Event = [];
    }

    eventsTimeConfigData.EventsTimeConfig.Server[sIdx].Event.push({
        $: { Key: "NewEventKey", StartDate: "2026-01-01 00:00:00", EndDate: "2026-12-31 23:59:59", Enable: "true", NewServerEvent: "false" }
    });

    renderGlobalTimesForm(eventsTimeConfigData);
}

function removeEvent(sIdx, eIdx) {
    eventsTimeConfigData.EventsTimeConfig.Server[sIdx].Event.splice(eIdx, 1);
    renderGlobalTimesForm(eventsTimeConfigData);
}

function getExportState() {
    return document.getElementById('exportModeToggle').checked;
}

async function saveGlobalTimes() {
    if (!eventsTimeConfigData) return;

    const updatedData = gatherGlobalTimesFormData();
    updatedData.isExport = getExportState();

    try {
        const response = await fetch('/api/events-time-config', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(updatedData)
        });

        const result = await response.json();
        if (response.ok) {
            showAlert(result.message, 'success');
        } else {
            throw new Error(result.error || 'Server error');
        }
    } catch (error) {
        console.error(error);
        showAlert('Lỗi khi lưu: ' + error.message, 'danger');
    }
}

// ----------------------------------------------------
// EVENT COMPOSE (MonthlyCompose.json)
// ----------------------------------------------------
let monthlyComposeData = null;

async function loadComposeTimes() {
    try {
        const endpoints = [
            { url: '/api/monthly-compose' },
            { url: '/api/general/script-items' },
            { url: '/api/general/randomboxes' }
        ];
        const responses = await Promise.all(endpoints.map(e => fetch(e.url)));

        monthlyComposeData = await responses[0].json();
        scriptItemsGeneral = await responses[1].json();
        randomboxesGeneral = await responses[2].json();

        renderComposeForm(monthlyComposeData);
    } catch (error) {
        console.error(error);
        document.getElementById('compose-form').innerHTML = '<p class="text-danger">Không thể tải dữ liệu.</p>';
    }
}

function renderComposeForm(data) {
    const container = document.getElementById('compose-form');

    let html = `
    <div class="card mb-3 border-info shadow-sm">
        <div class="card-header bg-info text-white py-1">
            <h6 class="fw-bold mb-0 small"><i class="bi bi-gear-fill"></i> Thông số Global (MonthlyCompose.json)</h6>
        </div>
        <div class="card-body py-2">
            <div class="row g-2">
                <div class="col-md-2">
                    <label class="form-label small fw-bold mb-1">Kỳ Trân Các (Đồng)</label>
                    <input type="number" class="form-control form-control-sm compose-global-attr" data-attr="TokenItem" value="${data.TokenItem || 0}">
                </div>
                <div class="col-md-2">
                    <label class="form-label small fw-bold mb-1">Giá bán (KNB)</label>
                    <input type="number" class="form-control form-control-sm compose-global-attr" data-attr="TokenItemPrice" value="${data.TokenItemPrice || 0}">
                </div>
                <div class="col-md-2">
                    <label class="form-label small fw-bold mb-1">Kỳ Trân Các (Đồng Khóa)</label>
                    <input type="number" class="form-control form-control-sm compose-global-attr" data-attr="TokenBindItem" value="${data.TokenBindItem || 0}">
                </div>
                <div class="col-md-2">
                    <label class="form-label small fw-bold mb-1">Giá Mảnh (Đồng Khóa)</label>
                    <input type="number" class="form-control form-control-sm compose-global-attr" data-attr="TokenBindItemPrice" value="${data.TokenBindItemPrice || 0}">
                </div>
                <div class="col-md-2">
                    <label class="form-label small fw-bold mb-1">G/h mua ngày (Đồng Khóa)</label>
                    <input type="number" class="form-control form-control-sm compose-global-attr" data-attr="TokenBindItemLimitPerDay" value="${data.TokenBindItemLimitPerDay || 0}">
                </div>
                <div class="col-md-2">
                    <label class="form-label small fw-bold mb-1">Lv quái tối thiểu rơi</label>
                    <input type="number" class="form-control form-control-sm compose-global-attr" data-attr="MonsterLevelLimit" value="${data.MonsterLevelLimit || 0}">
                </div>
            </div>
        </div>
    </div>
    <div class="row g-3">
        <!-- Section for Material Weight (MaterialRate) -->
        <div class="col-md-6">
            <div class="d-flex justify-content-between align-items-center mb-2 border-bottom pb-2">
                <h6 class="fw-bold mb-0 text-success"><i class="bi bi-recycle"></i> Trọng số rớt nguyên liệu (MaterialRate)</h6>
                <button class="btn btn-xs btn-success py-0 px-2" onclick="addComposeMaterial()"><i class="bi bi-plus"></i></button>
            </div>
            <div class="table-responsive" style="max-height: 400px;">
                <table class="table table-sm table-bordered align-middle" id="compose-material-table">
                    <thead class="table-light small">
                        <tr>
                            <th>Vật phẩm</th>
                            <th width="100">Trọng số</th>
                            <th width="40"></th>
                        </tr>
                    </thead>
                    <tbody>`;

    for (const itemId in data.MaterialRate) {
        const item = scriptItemsGeneral.find(i => i.ItemID == itemId);
        const itemName = item ? item.Name : 'Unknown Item';
        html += `
            <tr>
                <td>
                    <input type="number" class="form-control form-control-sm compose-mat-id" value="${itemId}">
                </td>
                <td><input type="number" class="form-control form-control-sm compose-mat-rate" value="${data.MaterialRate[itemId]}"></td>
                <td class="text-center"><button class="btn btn-link text-danger p-0" onclick="this.closest('tr').remove()"><i class="bi bi-trash"></i></button></td>
            </tr>
        `;
    }

    html += `</tbody></table></div></div>

        <!-- Section for Level/Server Drop Rate (DropRate) -->
        <div class="col-md-6">
            <div class="d-flex justify-content-between align-items-center mb-2 border-bottom pb-2">
                <h6 class="fw-bold mb-0 text-info"><i class="bi bi-percent"></i> Tỉ lệ rớt Nguyên liệu (DropRate)</h6>
                <button class="btn btn-xs btn-info py-0 px-2 text-white" onclick="addComposeDropRate()"><i class="bi bi-plus"></i></button>
            </div>
            <div class="table-responsive" style="max-height: 400px;">
                <table class="table table-sm table-bordered align-middle" id="compose-droprate-table">
                    <thead class="table-light small">
                        <tr>
                            <th>Server áp dụng (vd: 1-150)</th>
                            <th width="100">Tỉ lệ (%)</th>
                            <th width="40"></th>
                        </tr>
                    </thead>
                    <tbody>`;

    for (const levelRange in data.DropRate) {
        html += `
            <tr>
                <td><input type="text" class="form-control form-control-sm compose-drop-range" value="${levelRange}"></td>
                <td><input type="number" class="form-control form-control-sm compose-drop-rate" value="${data.DropRate[levelRange]}"></td>
                <td class="text-center"><button class="btn btn-link text-danger p-0" onclick="this.closest('tr').remove()"><i class="bi bi-trash"></i></button></td>
            </tr>
        `;
    }

    html += `</tbody></table></div></div>
    </div>`;
    container.innerHTML = html;
}

function addComposeMaterial() {
    const tbody = document.querySelector('#compose-material-table tbody');
    const tr = document.createElement('tr');
    tr.innerHTML = `
        <td>
            <input type="number" class="form-control form-control-sm compose-mat-id" value="0">
        </td>
        <td><input type="number" class="form-control form-control-sm compose-mat-rate" value="1"></td>
        <td class="text-center"><button class="btn btn-link text-danger p-0" onclick="this.closest('tr').remove()"><i class="bi bi-trash"></i></button></td>
    `;
    tbody.appendChild(tr);
}

function addComposeDropRate() {
    const tbody = document.querySelector('#compose-droprate-table tbody');
    const tr = document.createElement('tr');
    tr.innerHTML = `
        <td><input type="text" class="form-control form-control-sm compose-drop-range" value="1-150"></td>
        <td><input type="number" class="form-control form-control-sm compose-drop-rate" value="5"></td>
        <td class="text-center"><button class="btn btn-link text-danger p-0" onclick="this.closest('tr').remove()"><i class="bi bi-trash"></i></button></td>
    `;
    tbody.appendChild(tr);
}

// Material ID listener removed as per user request to only show IDs

async function saveComposeConfig() {
    if (!monthlyComposeData) return;

    try {
        // Collect MaterialRate
        const materialRate = {};
        document.querySelectorAll('#compose-material-table tbody tr').forEach(tr => {
            const id = tr.querySelector('.compose-mat-id').value;
            const rate = parseInt(tr.querySelector('.compose-mat-rate').value);
            if (id && !isNaN(rate)) materialRate[id] = rate;
        });

        // Collect DropRate
        const dropRate = {};
        document.querySelectorAll('#compose-droprate-table tbody tr').forEach(tr => {
            const range = tr.querySelector('.compose-drop-range').value;
            const rate = parseInt(tr.querySelector('.compose-drop-rate').value);
            if (range && !isNaN(rate)) dropRate[range] = rate;
        });

        monthlyComposeData.MaterialRate = materialRate;
        monthlyComposeData.DropRate = dropRate;
        
        // Collect Global Attributes
        document.querySelectorAll('.compose-global-attr').forEach(input => {
            const attr = input.dataset.attr;
            monthlyComposeData[attr] = parseInt(input.value) || 0;
        });

        monthlyComposeData.isExport = getExportState();

        const response = await fetch('/api/monthly-compose', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(monthlyComposeData)
        });

        const result = await response.json();
        if (response.ok) {
            showAlert(result.message, 'success');
            renderComposeForm(monthlyComposeData); // Refresh item names
        } else {
            throw new Error(result.error);
        }
    } catch (error) {
        showAlert('Lỗi khi lưu cấu hình: ' + error.message, 'danger');
    }
}

// ----------------------------------------------------
// SLOT MACHINE (SlotMachine.xml)
// ----------------------------------------------------
let slotMachineData = null;

async function loadSlotMachine() {
    try {
        const response = await fetch('/api/slot-machine');
        if (!response.ok) throw new Error('Failed to fetch data');

        slotMachineData = await response.json();
        renderSlotMachineForm(slotMachineData);
    } catch (error) {
        console.error(error);
        document.getElementById('slotmachine-form').innerHTML = '<p class="text-danger">Không thể tải dữ liệu XML SlotMachine.</p>';
    }
}

function renderSlotMachineForm(data) {
    const container = document.getElementById('slotmachine-form');
    if (!data.SlotMachineRewards || !data.SlotMachineRewards.NumberLimit) {
        // Init if empty
        if (!data.SlotMachineRewards) data.SlotMachineRewards = {};
        data.SlotMachineRewards.NumberLimit = [];
    }

    // xml2js trả về single-element là object thay vì array — cần normalize
    if (!Array.isArray(data.SlotMachineRewards.NumberLimit)) {
        data.SlotMachineRewards.NumberLimit = [data.SlotMachineRewards.NumberLimit];
    }

    const limits = data.SlotMachineRewards.NumberLimit;

    let html = `
        <table class="table table-bordered table-sm mt-3">
            <thead class="table-light">
                <tr>
                    <th>StartDate</th>
                    <th>EndDate</th>
                    <th>MaxNumber</th>
                    <th width="10%"></th>
                </tr>
            </thead>
            <tbody>
    `;

    limits.forEach((limitNode, index) => {
        const attr = limitNode.$ || {};
        html += `
            <tr>
                <td><input type="datetime-local" step="1" class="form-control form-control-sm sm-attr" data-idx="${index}" data-attr="StartDate" value="${formatToInput(attr.StartDate || '')}"></td>
                <td><input type="datetime-local" step="1" class="form-control form-control-sm sm-attr" data-idx="${index}" data-attr="EndDate" value="${formatToInput(attr.EndDate || '')}"></td>
                <td><input type="number" class="form-control form-control-sm sm-attr" data-idx="${index}" data-attr="MaxNumber" value="${attr.MaxNumber || ''}"></td>
                <td class="text-center">
                    <button class="btn btn-sm btn-outline-danger" onclick="removeSlotMachineLimit(${index})"><i class="bi bi-trash"></i></button>
                </td>
            </tr>
        `;
    });

    html += `</tbody></table>`;
    container.innerHTML = html;
}

function gatherSlotMachineData() {
    document.querySelectorAll('.sm-attr').forEach(input => {
        const idx = input.dataset.idx;
        const attrName = input.dataset.attr;
        let val = input.value;
        if (input.type === 'datetime-local') val = formatToXml(val);

        // Đảm bảo node và node.$ tồn tại trước khi ghi
        const node = slotMachineData.SlotMachineRewards.NumberLimit[idx];
        if (!node) return;
        if (!node.$) node.$ = {};
        node.$[attrName] = val;
    });
}

function addSlotMachineLimit() {
    gatherSlotMachineData();
    slotMachineData.SlotMachineRewards.NumberLimit.push({
        $: { StartDate: '2026-01-01 00:00:00', EndDate: '2026-12-31 23:59:59', MaxNumber: '99999' }
    });
    renderSlotMachineForm(slotMachineData);
}

function removeSlotMachineLimit(idx) {
    gatherSlotMachineData();
    slotMachineData.SlotMachineRewards.NumberLimit.splice(idx, 1);
    renderSlotMachineForm(slotMachineData);
}

async function saveSlotMachine() {
    if (!slotMachineData) return;
    gatherSlotMachineData();
    slotMachineData.isExport = getExportState();

    try {
        const response = await fetch('/api/slot-machine', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(slotMachineData)
        });
        const result = await response.json();
        if (response.ok) {
            showAlert('Lưu cấu hình Ngũ Phúc Lâm Môn thành công!', 'success');
        } else throw new Error(result.error);
    } catch (error) {
        showAlert('Lỗi: ' + error.message, 'danger');
    }
}

// ----------------------------------------------------
// RECHARGE EVENTS
// ----------------------------------------------------
// ----------------------------------------------------
// RECHARGE MANAGEMENT (NEW TAB)
// ----------------------------------------------------
let scriptItemsRecharge = [];
let randomboxesPackages = [];
let randomboxesMilestones = [];
let rechargeAwardData = null;

let currentEditingBox = null;
let currentEditingVariant = 'base'; // 'base' or 'server38'

async function loadRechargeManagement() {
    try {
        const endpoints = [
            { name: 'Script Items', url: '/api/recharge/script-items' },
            { name: 'Box Packages', url: '/api/recharge/randomboxes-packages' },
            { name: 'Box Milestones', url: '/api/recharge/randomboxes-milestones' },
            { name: 'Recharge Award', url: '/api/recharge-award' }
        ];

        const responses = await Promise.all(endpoints.map(e => fetch(e.url)));

        for (let i = 0; i < responses.length; i++) {
            if (!responses[i].ok) {
                let errorDetail = '';
                try {
                    const errorJson = await responses[i].json();
                    errorDetail = errorJson.error || JSON.stringify(errorJson);
                } catch (e) {
                    errorDetail = await responses[i].text();
                }
                throw new Error(`${endpoints[i].name} failed: ${errorDetail}`);
            }
        }

        scriptItemsRecharge = await responses[0].json();
        randomboxesPackages = await responses[1].json();
        randomboxesMilestones = await responses[2].json();
        rechargeAwardData = await responses[3].json();

        renderRechargeManagementSection();
    } catch (err) {
        console.error("Recharge Management load error:", err);
        showAlert('Lỗi khi tải dữ liệu Quản lý Ưu đãi & Tích lũy: ' + err.message, 'danger');
    }
}

function renderRechargeManagementSection() {
    renderRechargeAwardForm(rechargeAwardData);
    renderRechargeScriptItems();
    renderRandomBoxFile('randombox-packages-container', randomboxesPackages, 'DayRecharge');
    renderRandomBoxFile('randombox-milestones-container', randomboxesMilestones, 'AccRecharge');
}

// 1. Script Items Recharge (Table UI)
function renderRechargeScriptItems() {
    const container = document.getElementById('recharge-script-items-container');
    if (!scriptItemsRecharge || scriptItemsRecharge.length === 0) {
        container.innerHTML = '<p class="text-muted">Không có dữ liệu script items.</p>';
        return;
    }

    let html = `<div class="list-group list-group-flush border rounded" style="max-height: 400px; overflow-y: auto;">`;

    scriptItemsRecharge.forEach((item, idx) => {
        html += `
            <div class="list-group-item d-flex justify-content-between align-items-center py-2">
                <div>
                    <span class="badge bg-secondary me-2">${item.ItemID}</span>
                    <span class="fw-bold">${item.Name || 'Không tên'}</span>
                </div>
                <div class="btn-group">
                    <button class="btn btn-sm btn-outline-primary" onclick="openScriptItemModal(${idx})"><i class="bi bi-pencil"></i> Sửa</button>
                    <button class="btn btn-sm btn-outline-danger" onclick="deleteRechargeScriptItem(${idx})"><i class="bi bi-trash"></i></button>
                </div>
            </div>
        `;
    });

    html += `</div>`;
    container.innerHTML = html;
}

function openScriptItemModal(idx) {
    const item = scriptItemsRecharge[idx];
    document.getElementById('edit-script-item-idx').value = idx;
    document.getElementById('edit-script-item-id').value = item.ItemID;
    document.getElementById('edit-script-item-name').value = item.Name || '';
    document.getElementById('edit-script-item-level').value = item.Level || 0;
    document.getElementById('edit-script-item-stack').value = item.Stack || 1;
    document.getElementById('edit-script-item-value').value = item.ItemValue || 0;
    document.getElementById('edit-script-item-intro').value = item.Intro || '';

    renderModalOverrides(item.Override);

    const modal = new bootstrap.Modal(document.getElementById('scriptItemModal'));
    modal.show();
}

function renderModalOverrides(override) {
    const container = document.getElementById('edit-script-item-overrides');
    if (!override || Object.keys(override).length === 0) {
        container.innerHTML = '<small class="text-muted">Chưa có ghi đè server.</small>';
        return;
    }

    let html = '<div class="d-flex flex-column gap-2">';
    for (const key in override) {
        html += `
            <div class="input-group input-group-sm">
                <span class="input-group-text">Server/Khoảng</span>
                <input type="text" class="form-control modal-override-editor-key" value="${key}" data-old-key="${key}">
                <span class="input-group-text">Box ID</span>
                <input type="number" class="form-control modal-override-editor-val" value="${override[key].ItemValue}">
                <button class="btn btn-outline-danger" onclick="this.parentElement.remove()"><i class="bi bi-trash"></i></button>
            </div>
        `;
    }
    html += '</div>';
    container.innerHTML = html;
}

function addModalOverride() {
    const container = document.getElementById('edit-script-item-overrides');
    const div = document.createElement('div');
    div.className = 'input-group input-group-sm mt-2';
    div.innerHTML = `
        <span class="input-group-text">Server/Khoảng</span>
        <input type="text" class="form-control modal-override-editor-key" value="NewServer" data-old-key="NewServer">
        <span class="input-group-text">Box ID</span>
        <input type="number" class="form-control modal-override-editor-val" value="0">
        <button class="btn btn-outline-danger" onclick="this.parentElement.remove()"><i class="bi bi-trash"></i></button>
    `;
    if (container.querySelector('.text-muted')) container.innerHTML = '';
    container.appendChild(div);
}

function saveScriptItemModal() {
    const idx = document.getElementById('edit-script-item-idx').value;
    const item = scriptItemsRecharge[idx];

    item.ItemID = parseInt(document.getElementById('edit-script-item-id').value);
    item.Name = document.getElementById('edit-script-item-name').value;
    item.Level = parseInt(document.getElementById('edit-script-item-level').value);
    item.Stack = parseInt(document.getElementById('edit-script-item-stack').value);
    item.ItemValue = parseInt(document.getElementById('edit-script-item-value').value);
    item.Intro = document.getElementById('edit-script-item-intro').value;

    // Process overrides
    const newOverride = {};
    document.querySelectorAll('.modal-override-editor-key').forEach((keyInput, i) => {
        const valInput = document.querySelectorAll('.modal-override-editor-val')[i];
        if (keyInput && valInput) {
            newOverride[keyInput.value] = { ItemValue: parseInt(valInput.value) };
        }
    });
    item.Override = Object.keys(newOverride).length > 0 ? newOverride : null;

    renderRechargeScriptItems();
    bootstrap.Modal.getInstance(document.getElementById('scriptItemModal')).hide();
}

// Remove old override functions
function renderOverrideList() { return ''; }
function addOverride() { }
function removeOverride() { }
function gatherScriptItemsData() { }

function addRechargeScriptItem() {
    scriptItemsRecharge.push({ ItemID: 0, Name: "New Item", Level: 1, ItemValue: 0, Stack: 1, Intro: "", Override: null });
    renderRechargeScriptItems();
}

function deleteRechargeScriptItem(idx) {
    if (confirm('Xóa item này?')) {
        scriptItemsRecharge.splice(idx, 1);
        renderRechargeScriptItems();
    }
}

async function saveRechargeScriptItems() {
    try {
        const payload = { isExport: getExportState(), data: scriptItemsRecharge };
        const response = await fetch('/api/recharge/script-items', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if (response.ok) showAlert('Lưu script_items_recharge.json thành công!', 'success');
        else throw new Error(await response.text());
    } catch (err) {
        showAlert('Lỗi: ' + err.message, 'danger');
    }
}

// 2. Random Box Files
function renderRandomBoxFile(containerId, data, type) {
    const container = document.getElementById(containerId);
    if (!data || data.length === 0) {
        container.innerHTML = '<p class="text-muted">Không có dữ liệu box.</p>';
        return;
    }

    let html = `<div class="list-group list-group-flush" style="max-height: 400px; overflow-y: auto;">`;
    data.forEach(box => {
        const comment = box._comment ? ` <span class="text-muted small">(${box._comment})</span>` : '';
        html += `
            <div class="list-group-item d-flex justify-content-between align-items-center py-1">
                <span><b>${box.IDBox}</b>: ${box.BoxName || 'Không tên'}${comment}</span>
                <button class="btn btn-sm btn-outline-primary" onclick="openBoxEditor(${box.IDBox}, '${type}', true)"><i class="bi bi-pencil"></i> Sửa</button>
            </div>
        `;
    });
    html += `</div>`;
    container.innerHTML = html;
}

async function saveRandomBoxPackages() {
    try {
        const payload = { isExport: getExportState(), data: randomboxesPackages };
        const response = await fetch('/api/recharge/randomboxes-packages', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if (response.ok) showAlert('Lưu Packages JSON thành công!', 'success');
        else throw new Error(await response.text());
    } catch (err) { showAlert('Lỗi: ' + err.message, 'danger'); }
}

async function saveRandomBoxMilestones() {
    try {
        const payload = { isExport: getExportState(), data: randomboxesMilestones };
        const response = await fetch('/api/recharge/randomboxes-milestones', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if (response.ok) showAlert('Lưu Milestones JSON thành công!', 'success');
        else throw new Error(await response.text());
    } catch (err) { showAlert('Lỗi: ' + err.message, 'danger'); }
}

// Keep RechargeEvent_OldServer.xml loading
let rechargeXmlData = null;
async function loadRechargeXml() {
    try {
        const rXml = await fetch('/api/recharge-xml');
        if (rXml.ok) {
            rechargeXmlData = await rXml.json();
            renderRechargeXmlForm(rechargeXmlData);
        }
    } catch (err) { console.error("Recharge XML error:", err); }
}

// ----------------------------------------------------
// Nâng cấp: Chỉnh sửa Full cấu trúc RechargeEvent_OldServer.xml 
// ----------------------------------------------------

function renderRechargeXmlForm(data) {
    const container = document.getElementById('recharge-xml-form');
    if (!data.RechargeEvent || !data.RechargeEvent.RechargeMilestone) {
        container.innerHTML = '<p class="text-danger">Định dạng file XML không hợp lệ.</p>';
        return;
    }

    const milestones = data.RechargeEvent.RechargeMilestone;
    let html = '';

    milestones.forEach((mileNode, mIdx) => {
        const mAttr = mileNode.$ || {};
        html += `<div class="card mb-3 border-secondary">
            <div class="card-header bg-dark text-white d-flex justify-content-between align-items-center">
                <span class="fw-bold">
                    Tên Mốc: <input type="text" class="form-control d-inline w-auto form-control-sm m-attr" data-midx="${mIdx}" data-attr="Name" value="${mAttr.Name || ''}">
                    Giá trị (Price): <input type="number" class="form-control d-inline w-auto form-control-sm m-attr" data-midx="${mIdx}" data-attr="Price" value="${mAttr.Price || ''}">
                </span>
                <button class="btn btn-sm btn-danger" onclick="deleteRechargeMilestone(${mIdx})"><i class="bi bi-trash"></i> Xóa Mốc</button>
            </div>
            <div class="card-body bg-light">
                <div class="mb-2"><button class="btn btn-sm btn-outline-primary" onclick="addRechargePackage(${mIdx})"><i class="bi bi-plus"></i> Thêm Gói Quà (Package)</button></div>
                <div class="row">`;

        if (mileNode.Package && Array.isArray(mileNode.Package)) {
            mileNode.Package.forEach((pkgNode, pIdx) => {
                const pAttr = pkgNode.$ || {};
                html += `<div class="col-md-4 mb-2">
                    <div class="border p-2 bg-white rounded shadow-sm">
                        <div class="d-flex justify-content-between mb-2">
                            <strong>Gói (ID/Tên):</strong> 
                            <button class="btn btn-sm text-danger p-0" onclick="deleteRechargePackage(${mIdx}, ${pIdx})"><i class="bi bi-x-circle-fill"></i></button>
                        </div>
                        <div class="row mb-2">
                            <div class="col-4 px-1"><input type="number" class="form-control form-control-sm p-attr" data-midx="${mIdx}" data-pidx="${pIdx}" data-attr="ID" value="${pAttr.ID || ''}" placeholder="ID"></div>
                            <div class="col-8 px-1"><input type="text" class="form-control form-control-sm p-attr" data-midx="${mIdx}" data-pidx="${pIdx}" data-attr="Name" value="${pAttr.Name || ''}" placeholder="Name (Ngày 1)"></div>
                        </div>
                        
                        <div class="d-flex justify-content-between align-items-center bg-light px-1 py-1 rounded">
                            <span style="font-size: 0.85rem; font-weight: bold;">Items</span>
                            <button class="btn btn-xs btn-success py-0 px-1" style="font-size: 0.75rem;" onclick="addRechargeItem(${mIdx}, ${pIdx})"><i class="bi bi-plus"></i></button>
                        </div>
                        <ul class="list-unstyled mb-0 px-1 mt-1">`;

                if (pkgNode.Item && Array.isArray(pkgNode.Item)) {
                    pkgNode.Item.forEach((itemNode, iIdx) => {
                        const iAttr = itemNode.$ || {};
                        html += `
                            <li class="d-flex mb-1 gap-1">
                                <input type="number" class="form-control form-control-sm i-attr" placeholder="Item ID" data-midx="${mIdx}" data-pidx="${pIdx}" data-iidx="${iIdx}" data-attr="ID" value="${iAttr.ID || ''}">
                                <input type="number" class="form-control form-control-sm i-attr" placeholder="Qty" data-midx="${mIdx}" data-pidx="${pIdx}" data-iidx="${iIdx}" data-attr="Count" value="${iAttr.Count || ''}">
                                <button class="btn btn-sm btn-outline-danger py-0 px-2" onclick="deleteRechargeItem(${mIdx}, ${pIdx}, ${iIdx})"><i class="bi bi-x"></i></button>
                            </li>`;
                    });
                }
                html += `</ul></div></div>`;
            });
        }
        html += `</div></div></div>`;
    });

    container.innerHTML = html;
}

function gatherRechargeXmlData() {
    document.querySelectorAll('.m-attr').forEach(input => {
        rechargeXmlData.RechargeEvent.RechargeMilestone[input.dataset.midx].$[input.dataset.attr] = input.value;
    });
    document.querySelectorAll('.p-attr').forEach(input => {
        rechargeXmlData.RechargeEvent.RechargeMilestone[input.dataset.midx].Package[input.dataset.pidx].$[input.dataset.attr] = input.value;
    });
    document.querySelectorAll('.i-attr').forEach(input => {
        rechargeXmlData.RechargeEvent.RechargeMilestone[input.dataset.midx].Package[input.dataset.pidx].Item[input.dataset.iidx].$[input.dataset.attr] = input.value;
    });
}

function addRechargeMilestone() {
    gatherRechargeXmlData();
    if (!rechargeXmlData.RechargeEvent.RechargeMilestone) rechargeXmlData.RechargeEvent.RechargeMilestone = [];
    rechargeXmlData.RechargeEvent.RechargeMilestone.push({
        $: { Name: "New Milestone", Price: "1000" },
        Package: []
    });
    renderRechargeXmlForm(rechargeXmlData);
}

function deleteRechargeMilestone(mIdx) {
    gatherRechargeXmlData();
    rechargeXmlData.RechargeEvent.RechargeMilestone.splice(mIdx, 1);
    renderRechargeXmlForm(rechargeXmlData);
}

function addRechargePackage(mIdx) {
    gatherRechargeXmlData();
    if (!rechargeXmlData.RechargeEvent.RechargeMilestone[mIdx].Package) rechargeXmlData.RechargeEvent.RechargeMilestone[mIdx].Package = [];
    rechargeXmlData.RechargeEvent.RechargeMilestone[mIdx].Package.push({
        $: { ID: "1", Name: "Ngày X" },
        Item: []
    });
    renderRechargeXmlForm(rechargeXmlData);
}

function deleteRechargePackage(mIdx, pIdx) {
    gatherRechargeXmlData();
    rechargeXmlData.RechargeEvent.RechargeMilestone[mIdx].Package.splice(pIdx, 1);
    renderRechargeXmlForm(rechargeXmlData);
}

function addRechargeItem(mIdx, pIdx) {
    gatherRechargeXmlData();
    if (!rechargeXmlData.RechargeEvent.RechargeMilestone[mIdx].Package[pIdx].Item) rechargeXmlData.RechargeEvent.RechargeMilestone[mIdx].Package[pIdx].Item = [];
    rechargeXmlData.RechargeEvent.RechargeMilestone[mIdx].Package[pIdx].Item.push({
        $: { ID: "506", Count: "100" }
    });
    renderRechargeXmlForm(rechargeXmlData);
}

function deleteRechargeItem(mIdx, pIdx, iIdx) {
    gatherRechargeXmlData();
    rechargeXmlData.RechargeEvent.RechargeMilestone[mIdx].Package[pIdx].Item.splice(iIdx, 1);
    renderRechargeXmlForm(rechargeXmlData);
}

async function saveRechargeXml() {
    if (!rechargeXmlData) return;
    gatherRechargeXmlData();
    rechargeXmlData.isExport = getExportState();

    try {
        const response = await fetch('/api/recharge-xml', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(rechargeXmlData)
        });
        const result = await response.json();
        if (response.ok) {
            showAlert('Lưu Gói Quà Nạp (XML) thành công!', 'success');
        } else throw new Error(result.error);
    } catch (error) {
        showAlert('Lỗi XML: ' + error.message, 'danger');
    }
}
// ----------------------------------------------------
// CHECK-IN (CheckIn.xml)
// ----------------------------------------------------
let checkInData = null;

async function loadCheckIn() {
    try {
        const response = await fetch('/api/checkin');
        if (!response.ok) throw new Error('Failed to fetch data');
        checkInData = await response.json();
        renderCheckInForm(checkInData);
    } catch (error) {
        document.getElementById('checkin-form').innerHTML = '<p class="text-danger">Lỗi tải dữ liệu.</p>';
    }
}

function renderCheckInForm(data) {
    const container = document.getElementById('checkin-form');
    if (!data.CheckInEvent || !data.CheckInEvent.CheckInPackage) return;

    let html = '';
    data.CheckInEvent.CheckInPackage.forEach((pkg, pIdx) => {
        const pAttr = pkg.$ || {};
        const moneyType = parseInt(pAttr.Money || "1");
        html += `<div class="card mb-3 border-info">
            <div class="card-header bg-info text-white">
                Gói: <b>${pAttr.Name}</b> | 
                Giá: <input type="number" class="form-control d-inline w-auto form-control-sm ci-attr" data-pidx="${pIdx}" data-attr="Price" value="${pAttr.Price || ''}"> 
                <select class="form-select form-select-sm d-inline ci-attr" style="width: auto; min-width: 80px;" data-pidx="${pIdx}" data-attr="Money">
                    <option value="1" ${moneyType === 1 ? 'selected' : ''}>Bạc</option>
                    <option value="2" ${moneyType === 2 ? 'selected' : ''}>KNB</option>
                </select>
            </div>
            <div class="card-body">
                <div class="row">`;

        // Items
        html += `<div class="col-md-6 border-end"><h6>Items Điểm Danh Tuần</h6><ul class="list-unstyled mb-0">`;
        if (pkg.Item) {
            pkg.Item.forEach((item, iIdx) => {
                const iAttr = item.$ || {};
                html += `<li class="d-flex mb-1 gap-1">ID: <input type="number" class="form-control form-control-sm ci-i-attr" data-pidx="${pIdx}" data-iidx="${iIdx}" data-attr="ID" value="${iAttr.ID}"> Qty: <input type="number" class="form-control form-control-sm ci-i-attr" data-pidx="${pIdx}" data-iidx="${iIdx}" data-attr="Count" value="${iAttr.Count}"></li>`;
            });
        }
        html += `</ul></div>`;

        // Accumulated
        html += `<div class="col-md-6"><h6>Quà Tích Lũy Bổ Sung</h6><ul class="list-unstyled mb-0">`;
        if (pkg.AccumulatedReward) {
            pkg.AccumulatedReward.forEach((acc, aIdx) => {
                const aAttr = acc.$ || {};
                html += `<li class="d-flex mb-1 gap-1">Mốc(Ngày): <input type="number" class="form-control form-control-sm ci-a-attr" data-pidx="${pIdx}" data-aidx="${aIdx}" data-attr="Count" value="${aAttr.Count}"> Item ID: <input type="number" class="form-control form-control-sm ci-a-attr" data-pidx="${pIdx}" data-aidx="${aIdx}" data-attr="ID" value="${aAttr.ID}"></li>`;
            });
        }
        html += `</ul></div></div></div></div>`;
    });
    container.innerHTML = html;
}

function gatherCheckInData() {
    document.querySelectorAll('.ci-attr').forEach(input => {
        checkInData.CheckInEvent.CheckInPackage[input.dataset.pidx].$[input.dataset.attr] = input.value;
    });
    document.querySelectorAll('.ci-i-attr').forEach(input => {
        checkInData.CheckInEvent.CheckInPackage[input.dataset.pidx].Item[input.dataset.iidx].$[input.dataset.attr] = input.value;
    });
    document.querySelectorAll('.ci-a-attr').forEach(input => {
        checkInData.CheckInEvent.CheckInPackage[input.dataset.pidx].AccumulatedReward[input.dataset.aidx].$[input.dataset.attr] = input.value;
    });
}

async function saveCheckIn() {
    if (!checkInData) return;
    gatherCheckInData();
    checkInData.isExport = getExportState();
    try {
        const response = await fetch('/api/checkin', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(checkInData)
        });
        if (response.ok) showAlert('Lưu Cấu Hình Check-In thành công!', 'success');
        else throw new Error(await response.text());
    } catch (err) { showAlert('Lỗi: ' + err.message, 'danger'); }
}

// ----------------------------------------------------
// SEASHELL CIRCLE (Bách Bảo Rương)
// ----------------------------------------------------
let seashellData = null;

async function loadSeashell() {
    try {
        const endpoints = [
            { url: '/api/seashell' },
            { url: '/api/general/script-items' },
            { url: '/api/general/randomboxes' }
        ];
        const responses = await Promise.all(endpoints.map(e => fetch(e.url)));

        seashellData = await responses[0].json();
        scriptItemsGeneral = await responses[1].json();
        randomboxesGeneral = await responses[2].json();

        renderSeashellForm(seashellData);
    } catch (error) {
        console.error("Seashell Circle load error:", error);
        document.getElementById('seashell-form').innerHTML = '<p class="text-danger">Lỗi tải dữ liệu Bách Bảo Rương.</p>';
    }
}

function renderSeashellForm(data) {
    const container = document.getElementById('seashell-form');
    if (!data.SeashellCircle) return;

    const rootAttr = data.SeashellCircle.$ || {};

    let html = `
        <div class="row g-3 mb-4 p-3 bg-light border rounded shadow-sm">
            <h6 class="fw-bold mb-3 border-bottom pb-2 text-primary"><i class="bi bi-gear"></i> Thông số cấu hình chung</h6>
            <div class="col-md-3">
                <label class="form-label small">ID vé bách bảo rương (SeashellID)</label>
                <input type="number" class="form-control form-control-sm ss-root-attr" data-attr="SeashellID" value="${rootAttr.SeashellID}">
            </div>
            <div class="col-md-3">
                <label class="form-label small">ID Rương bách bảo (TreasureID)</label>
                <div class="input-group input-group-sm">
                    <input type="number" class="form-control ss-root-attr" data-attr="TreasureID" value="${rootAttr.TreasureID}">
                    <button class="btn btn-outline-warning" onclick="openGeneralBoxEditor(${rootAttr.TreasureID})"><i class="bi bi-gift"></i></button>
                </div>
            </div>
            <div class="col-md-3">
                <label class="form-label small">ID nguyên liệu event (EventMaterialID)</label>
                <input type="number" class="form-control form-control-sm ss-root-attr" data-attr="EventMaterialID" value="${rootAttr.EventMaterialID}">
            </div>
            <div class="col-md-3">
                <label class="form-label small">Vị trí rương (TreasurePosition)</label>
                <input type="text" class="form-control form-control-sm ss-root-attr" data-attr="TreasurePosition" value="${rootAttr.TreasurePosition}">
            </div>
            
            <div class="col-md-2">
                <label class="form-label small">Time Min (ms)</label>
                <input type="number" class="form-control form-control-sm ss-root-attr" data-attr="MinTimeTick" value="${rootAttr.MinTimeTick}">
            </div>
            <div class="col-md-2">
                <label class="form-label small">Time Max (ms)</label>
                <input type="number" class="form-control form-control-sm ss-root-attr" data-attr="MaxTimeTick" value="${rootAttr.MaxTimeTick}">
            </div>
            <div class="col-md-2">
                <label class="form-label small">Vòng Min</label>
                <input type="number" class="form-control form-control-sm ss-root-attr" data-attr="MinRound" value="${rootAttr.MinRound}">
            </div>
            <div class="col-md-2">
                <label class="form-label small">Vòng Max</label>
                <input type="number" class="form-control form-control-sm ss-root-attr" data-attr="MaxRound" value="${rootAttr.MaxRound}">
            </div>
            <div class="col-md-2">
                <label class="form-label small">Tỷ lệ Rương (n/1000)</label>
                <input type="number" class="form-control form-control-sm ss-root-attr" data-attr="RateToTreasure" value="${rootAttr.RateToTreasure}">
            </div>
            <div class="col-md-2">
                <label class="form-label small">ShopID / CouponID</label>
                <div class="input-group input-group-sm">
                    <input type="number" class="form-control ss-root-attr" data-attr="ShopID" value="${rootAttr.ShopID}">
                    <input type="number" class="form-control ss-root-attr" data-attr="CouponID" value="${rootAttr.CouponID}">
                </div>
            </div>
        </div>

        <ul class="nav nav-tabs mb-3" id="seashellTabs" role="tablist">
          <li class="nav-item"><button class="nav-link active py-1" data-bs-toggle="tab" data-bs-target="#ss-cells">Ô vật phẩm (Cells)</button></li>
          <li class="nav-item"><button class="nav-link py-1" data-bs-toggle="tab" data-bs-target="#ss-rates">Tỷ lệ quay (TurnRate)</button></li>
          <li class="nav-item"><button class="nav-link py-1" data-bs-toggle="tab" data-bs-target="#ss-exchange">Đổi Sò & Cược</button></li>
        </ul>

        <div class="tab-content border p-3 rounded bg-white shadow-sm">
            <div class="tab-pane fade show active" id="ss-cells">
                <div class="table-responsive">
                    <table class="table table-sm table-hover table-bordered align-middle mb-0">
                        <thead class="table-dark small">
                            <tr>
                                <th style="min-width: 220px;">Tên Ô (Name)</th>
                                <th>StageValues (Phân cách ;)</th>
                                <th width="120">Pos 1 (1 sao)</th>
                                <th width="120">Pos 2 (2 sao)</th>
                                <th width="120">Pos 3 (3 sao)</th>
                            </tr>
                        </thead>
                        <tbody>`;

    if (data.SeashellCircle.Cells && data.SeashellCircle.Cells[0].Cell) {
        data.SeashellCircle.Cells[0].Cell.forEach((cell, cIdx) => {
            const cAttr = cell.$ || {};
            html += `<tr>
                <td class="small fw-bold">
                    <input type="text" class="form-control form-control-sm ss-cell-attr" data-cidx="${cIdx}" data-attr="Name" value="${cAttr.Name || ''}">
                </td>
                <td><input type="text" class="form-control form-control-sm ss-cell-attr" data-cidx="${cIdx}" data-attr="StageValues" value="${cAttr.StageValues || ''}"></td>
                <td><input type="text" class="form-control form-control-sm ss-cell-attr" data-cidx="${cIdx}" data-attr="Position_1" value="${cAttr.Position_1 || ''}"></td>
                <td><input type="text" class="form-control form-control-sm ss-cell-attr" data-cidx="${cIdx}" data-attr="Position_2" value="${cAttr.Position_2 || ''}"></td>
                <td><input type="text" class="form-control form-control-sm ss-cell-attr" data-cidx="${cIdx}" data-attr="Position_3" value="${cAttr.Position_3 || ''}"></td>
            </tr>`;
        });
    }
    html += `</tbody></table></div></div>
            
            <div class="tab-pane fade" id="ss-rates">
                <div class="alert alert-warning py-1 small mb-2"><i class="bi bi-exclamation-triangle"></i> Lưu ý: Tổng Tỷ lệ (Pos 1+2+3) của mỗi tầng phải bằng 1000.</div>
                <table class="table table-sm table-bordered mb-0 align-middle">
                    <thead class="table-secondary small text-center">
                        <tr>
                            <th width="80">Tầng (ID)</th>
                            <th>Tỷ lệ ô cùng loại (RateToSameType)</th>
                            <th>Tỷ lệ 1 sao (Pos 1)</th>
                            <th>Tỷ lệ 2 sao (Pos 2)</th>
                            <th>Tỷ lệ 3 sao (Pos 3)</th>
                        </tr>
                    </thead>
                    <tbody>`;
    if (data.SeashellCircle.TurnRate && data.SeashellCircle.TurnRate[0].Stage) {
        data.SeashellCircle.TurnRate[0].Stage.forEach((stage, rIdx) => {
            const rAttr = stage.$ || {};
            html += `<tr>
                <td class="text-center fw-bold">Stage ${rAttr.ID}</td>
                <td><input type="number" class="form-control form-control-sm ss-rate-attr" data-ridx="${rIdx}" data-attr="RateToSameType" value="${rAttr.RateToSameType}"></td>
                <td><input type="number" class="form-control form-control-sm ss-rate-attr" data-ridx="${rIdx}" data-attr="RateToPosition_1" value="${rAttr.RateToPosition_1}"></td>
                <td><input type="number" class="form-control form-control-sm ss-rate-attr" data-ridx="${rIdx}" data-attr="RateToPosition_2" value="${rAttr.RateToPosition_2}"></td>
                <td><input type="number" class="form-control form-control-sm ss-rate-attr" data-ridx="${rIdx}" data-attr="RateToPosition_3" value="${rAttr.RateToPosition_3}"></td>
            </tr>`;
        });
    }
    html += `</tbody></table></div>
            
            <div class="tab-pane fade" id="ss-exchange">
                <h6 class="fw-bold small mb-2">Tỷ lệ đổi Sò (ExchangeSeashell)</h6>
                <div class="row row-cols-3 g-2 mb-3">`;
    if (data.SeashellCircle.ExchangeSeashell && data.SeashellCircle.ExchangeSeashell[0].Stage) {
        data.SeashellCircle.ExchangeSeashell[0].Stage.forEach((stage, exIdx) => {
            const exAttr = stage.$ || {};
            html += `<div class="col">
                <div class="input-group input-group-sm">
                    <span class="input-group-text d-inline-block text-truncate" style="max-width: 100px" title="Stage ${exAttr.ID}">Stage ${exAttr.ID}</span>
                    <input type="number" class="form-control ss-ex-attr" data-exidx="${exIdx}" data-attr="Value" value="${exAttr.Value}" placeholder="Value">
                    <input type="number" class="form-control ss-ex-attr" data-exidx="${exIdx}" data-attr="Score" value="${exAttr.Score}" placeholder="Score">
                </div>
            </div>`;
        });
    }
    html += `</div>
                <h6 class="fw-bold small mb-2 border-top pt-2">Tỷ lệ Cược (BetRate)</h6>
                <div class="row g-2">`;
    const bAttr = (data.SeashellCircle.BetRate && data.SeashellCircle.BetRate[0].$) || {};
    ['Bet_2', 'Bet_10', 'Bet_50'].forEach(key => {
        html += `<div class="col-md-4">
            <div class="input-group input-group-sm">
                <span class="input-group-text">Hệ số ${key.replace('Bet_', '')}</span>
                <input type="number" class="form-control ss-bet-attr" data-attr="${key}" value="${bAttr[key] || 0}">
            </div>
        </div>`;
    });
    html += `</div></div></div>`;

    container.innerHTML = html;
}

function gatherSeashellData() {
    // Root Attributes
    document.querySelectorAll('.ss-root-attr').forEach(input => {
        seashellData.SeashellCircle.$[input.dataset.attr] = input.value;
    });
    // Cells
    document.querySelectorAll('.ss-cell-attr').forEach(input => {
        seashellData.SeashellCircle.Cells[0].Cell[input.dataset.cidx].$[input.dataset.attr] = input.value;
    });
    // TurnRate
    document.querySelectorAll('.ss-rate-attr').forEach(input => {
        seashellData.SeashellCircle.TurnRate[0].Stage[input.dataset.ridx].$[input.dataset.attr] = input.value;
    });
    // ExchangeSeashell
    document.querySelectorAll('.ss-ex-attr').forEach(input => {
        seashellData.SeashellCircle.ExchangeSeashell[0].Stage[input.dataset.exidx].$[input.dataset.attr] = input.value;
    });
    // BetRate
    document.querySelectorAll('.ss-bet-attr').forEach(input => {
        if (!seashellData.SeashellCircle.BetRate[0].$) seashellData.SeashellCircle.BetRate[0].$ = {};
        seashellData.SeashellCircle.BetRate[0].$[input.dataset.attr] = input.value;
    });
}

async function saveSeashell() {
    if (!seashellData) return;
    gatherSeashellData();
    seashellData.isExport = getExportState();
    try {
        const response = await fetch('/api/seashell', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(seashellData)
        });
        if (response.ok) showAlert('Lưu Cấu Hình BBR thành công!', 'success');
        else throw new Error(await response.text());
    } catch (err) { showAlert('Lỗi: ' + err.message, 'danger'); }
}

// ----------------------------------------------------
// FLOWER TOWER (Thiên Tầng Tháp)
// ----------------------------------------------------
let flowerData = null;

async function loadFlowerTower() {
    try {
        const endpoints = [
            { url: '/api/flowertower' },
            { url: '/api/flowertower/script-items' },
            { url: '/api/flowertower/randomboxes' }
        ];
        const responses = await Promise.all(endpoints.map(e => fetch(e.url)));

        flowerData = await responses[0].json();
        scriptItemsFlowerTower = await responses[1].json();
        randomboxesFlowerTower = await responses[2].json();

        renderFlowerTowerForm(flowerData);
    } catch (error) {
        console.error("Flower Tower load error:", error);
        document.getElementById('flowertower-form').innerHTML = '<p class="text-danger">Lỗi tải dữ liệu Thiên Tầng Tháp.</p>';
    }
}

function renderFlowerTowerForm(data) {
    const container = document.getElementById('flowertower-form');
    if (!data.FlowerTower || !data.FlowerTower.Floor) return;

    let html = `
        <div class="d-flex justify-content-between align-items-center mb-3">
            <h5>Danh sách các Tầng & Nhiệm vụ</h5>
            <div class="btn-group">
                <button class="btn btn-sm btn-outline-primary" onclick="openFlowerRewardManager()">Quản lý Quà TTT</button>
            </div>
        </div>
    `;

    data.FlowerTower.Floor.forEach((floor, fIdx) => {
        const attr = floor.$ || {};
        const rewardId = attr.RewardID;

        html += `<div class="card mb-4 border-secondary shadow-sm">
            <div class="card-header py-2 bg-secondary text-white d-flex justify-content-between align-items-center">
                <span class="fw-bold">Tầng ${attr.id}</span>
                <div class="d-flex align-items-center gap-2">
                    <span class="small">RewardID:</span>
                    <input type="number" class="form-control form-control-sm ft-attr d-inline-block" style="width: 80px" data-fidx="${fIdx}" data-attr="RewardID" value="${rewardId}">
                    <button class="btn btn-xs btn-warning py-0" onclick="openFlowerBoxEditor(${rewardId})"><i class="bi bi-gift"></i> Sửa Quà</button>
                    <button class="btn btn-xs btn-success py-0" onclick="addFlowerTask(${fIdx})"><i class="bi bi-plus-circle"></i> Thêm Nhiệm vụ</button>
                </div>
            </div>
            <div class="card-body p-0">
                <div class="table-responsive">
                    <table class="table table-sm table-hover mb-0">
                        <thead class="table-light">
                            <tr>
                                <th width="50" class="text-center">ID</th>
                                <th>Mô tả (Description)</th>
                                <th width="100">Mục tiêu</th>
                                <th style="min-width: 300px;">Loại (EventType)</th>
                                <th width="120">Tham số (Param)</th>
                                <th width="150">Giá bỏ qua (Cost)</th>
                                <th width="50"></th>
                            </tr>
                        </thead>
                        <tbody>`;

        if (floor.Task) {
            const tasks = Array.isArray(floor.Task) ? floor.Task : [floor.Task];
            tasks.forEach((task, tIdx) => {
                const tAttr = task.$ || {};
                html += `<tr>
                    <td class="text-center align-middle small text-muted">${tAttr.id}</td>
                    <td><input type="text" class="form-control form-control-sm ft-t-attr" data-fidx="${fIdx}" data-tidx="${tIdx}" data-attr="Description" value="${tAttr.Description || ''}"></td>
                    <td><input type="number" class="form-control form-control-sm ft-t-attr" data-fidx="${fIdx}" data-tidx="${tIdx}" data-attr="Goal" value="${tAttr.Goal || 0}"></td>
                    <td>
                        <select class="form-select form-select-sm ft-t-attr" data-fidx="${fIdx}" data-tidx="${tIdx}" data-attr="EventType">
                            ${FLOWER_EVENT_TYPES.map(type => `<option value="${type.id}" ${tAttr.EventType == type.id ? 'selected' : ''}>${type.name} (${type.id})</option>`).join('')}
                            <option value="${tAttr.EventType}" ${!FLOWER_EVENT_TYPES.find(x => x.id == tAttr.EventType) ? 'selected' : ''}>Loại khác (${tAttr.EventType})</option>
                        </select>
                    </td>
                    <td><input type="text" class="form-control form-control-sm ft-t-attr" data-fidx="${fIdx}" data-tidx="${tIdx}" data-attr="Param" value="${tAttr.Param || '-1'}"></td>
                    <td><input type="number" class="form-control form-control-sm ft-t-attr" data-fidx="${fIdx}" data-tidx="${tIdx}" data-attr="Cost" value="${tAttr.Cost || -1}"></td>
                    <td class="text-center">
                        <button class="btn btn-link text-danger p-0" onclick="deleteFlowerTask(${fIdx}, ${tIdx})"><i class="bi bi-trash"></i></button>
                    </td>
                </tr>`;
            });
        }

        html += `</tbody></table></div></div></div>`;
    });
    container.innerHTML = html;
}

function addFlowerTask(fIdx) {
    gatherFlowerTowerData();

    if (!flowerData.FlowerTower.Floor[fIdx].Task) {
        flowerData.FlowerTower.Floor[fIdx].Task = [];
    }

    // Find max ID to auto increment
    let maxId = 0;
    flowerData.FlowerTower.Floor.forEach(f => {
        if (f.Task) {
            const tasks = Array.isArray(f.Task) ? f.Task : [f.Task];
            tasks.forEach(t => {
                const tid = parseInt(t.$.id);
                if (tid > maxId) maxId = tid;
            });
        }
    });

    const newTask = {
        $: { id: (maxId + 1).toString(), Description: "Nhiệm vụ mới", Goal: "1", Cost: "-1", EventType: "10002", Param: "-1" }
    };

    if (!Array.isArray(flowerData.FlowerTower.Floor[fIdx].Task)) {
        flowerData.FlowerTower.Floor[fIdx].Task = [flowerData.FlowerTower.Floor[fIdx].Task];
    }

    flowerData.FlowerTower.Floor[fIdx].Task.push(newTask);
    renderFlowerTowerForm(flowerData);
}

function deleteFlowerTask(fIdx, tIdx) {
    if (confirm('Bạn có chắc muốn xóa nhiệm vụ này?')) {
        gatherFlowerTowerData();
        flowerData.FlowerTower.Floor[fIdx].Task.splice(tIdx, 1);
        renderFlowerTowerForm(flowerData);
    }
}

function gatherFlowerTowerData() {
    // Collect Floor attributes
    document.querySelectorAll('.ft-attr').forEach(input => {
        flowerData.FlowerTower.Floor[input.dataset.fidx].$[input.dataset.attr] = input.value;
    });
    // Collect Task attributes
    document.querySelectorAll('.ft-t-attr').forEach(input => {
        flowerData.FlowerTower.Floor[input.dataset.fidx].Task[input.dataset.tidx].$[input.dataset.attr] = input.value;
    });
}

async function saveFlowerTower() {
    if (!flowerData) return;
    gatherFlowerTowerData();
    flowerData.isExport = getExportState();

    try {
        const response = await fetch('/api/flowertower', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(flowerData)
        });

        const result = await response.json();
        if (response.ok) {
            showAlert(result.message, 'success');
        } else {
            throw new Error(result.error);
        }
    } catch (error) {
        showAlert('Lỗi khi lưu cấu hình: ' + error.message, 'danger');
    }
}

function openFlowerBoxEditor(rewardId) {
    // We need to hunt down the IDBox from script_items_flowertower.json
    const scriptItem = scriptItemsFlowerTower.find(i => Number(i.ItemID) === Number(rewardId));
    if (!scriptItem) {
        alert(`Không tìm thấy Script Item cho RewardID ${rewardId} trong script_items_flowertower.json`);
        return;
    }

    const boxId = scriptItem.ItemValue;
    openBoxEditor(boxId, 'FlowerTower', true);
}

function openFlowerRewardManager() {
    // Show a simplified manager for Flower Tower script items
    // For now, let's just show an alert or a simple list if needed, 
    // but the user mostly wants to edit tasks and box rewards.
    // We'll focus on the Task & Box Reward integration first.
    showAlert('Chức năng quản lý danh sách hòm quà TTT đang được cập nhật.', 'info');
}

function openGeneralBoxEditor(rewardId) {
    const scriptItem = scriptItemsGeneral.find(i => Number(i.ItemID) === Number(rewardId));
    if (!scriptItem) {
        alert(`Không tìm thấy Script Item cho ID ${rewardId} trong script_items.json`);
        return;
    }

    const boxId = scriptItem.ItemValue;
    openBoxEditor(boxId, 'General', true);
}

// ----------------------------------------------------
// EXCHANGE TICKET (ExchangeTicketEvent.xml)
// ----------------------------------------------------
let exchangeTicketData = null;

async function loadExchangeTicket() {
    try {
        const response = await fetch('/api/exchange-ticket');
        if (!response.ok) throw new Error('Failed to fetch data');
        exchangeTicketData = await response.json();
        renderExchangeTicketForm(exchangeTicketData);
    } catch (error) {
        document.getElementById('exchange-ticket-form').innerHTML = '<p class="text-danger">Lỗi tải dữ liệu.</p>';
    }
}

function renderExchangeTicketForm(data) {
    const container = document.getElementById('exchange-ticket-form');
    if (!data.ExchangeTicket || !data.ExchangeTicket.Exchange) return;

    let html = '';
    data.ExchangeTicket.Exchange.forEach((ex, idx) => {
        const attr = ex.$ || {};
        html += `<div class="card mb-3 border-info">
            <div class="card-header bg-info bg-opacity-10 d-flex justify-content-between align-items-center py-2">
                <span class="fw-bold text-info">Cấu hình #${idx + 1}: ${attr.Name || 'Chưa đặt tên'}</span>
                <button class="btn btn-sm btn-outline-danger" onclick="deleteExchangeTicketConfig(${idx})"><i class="bi bi-trash"></i> Xóa</button>
            </div>
            <div class="card-body py-2">
                <div class="row g-2 mb-2">
                    <div class="col-md-3">
                        <label class="small fw-bold">TimeKey</label>
                        <input type="text" class="form-control form-control-sm et-attr" data-idx="${idx}" data-attr="TimeKey" value="${attr.TimeKey || ''}" placeholder="Vd: SlotMachine_Exchange">
                    </div>
                    <div class="col-md-5">
                        <label class="small fw-bold">Tên hiển thị (Name)</label>
                        <input type="text" class="form-control form-control-sm et-attr" data-idx="${idx}" data-attr="Name" value="${attr.Name || ''}" placeholder="Vd: Chuyển xu nhận quà">
                    </div>
                    <div class="col-md-2">
                        <label class="small fw-bold">Tỉ lệ KNB (RateKNB)</label>
                        <input type="number" class="form-control form-control-sm et-attr" data-idx="${idx}" data-attr="RateKNB" value="${attr.RateKNB || 100}">
                    </div>
                    <div class="col-md-2">
                        <label class="small fw-bold">ID Vật phẩm</label>
                        <input type="number" class="form-control form-control-sm et-attr" data-idx="${idx}" data-attr="ItemID" value="${attr.ItemID || 0}">
                    </div>
                    <div class="col-md-2">
                        <label class="small fw-bold">Số lượng (ItemCount)</label>
                        <input type="number" class="form-control form-control-sm et-attr" data-idx="${idx}" data-attr="ItemCount" value="${attr.ItemCount || 1}">
                    </div>
                    <div class="col-md-10">
                        <label class="small fw-bold">Tin nhắn thông báo (Message)</label>
                        <input type="text" class="form-control form-control-sm et-attr" data-idx="${idx}" data-attr="Message" value="${attr.Message || ''}">
                    </div>
                </div>
            </div>
        </div>`;
    });

    if (data.ExchangeTicket.Exchange.length === 0) {
        html = '<p class="text-muted p-3">Chưa có cấu hình nào. Bấm "Thêm cấu hình mới" để bắt đầu.</p>';
    }
    container.innerHTML = html;
}

function gatherExchangeTicketData() {
    document.querySelectorAll('.et-attr').forEach(input => {
        const idx = input.dataset.idx;
        const attr = input.dataset.attr;
        if (!exchangeTicketData.ExchangeTicket.Exchange[idx].$) exchangeTicketData.ExchangeTicket.Exchange[idx].$ = {};
        exchangeTicketData.ExchangeTicket.Exchange[idx].$[attr] = input.value;
    });
}

function addExchangeTicketConfig() {
    if (!exchangeTicketData.ExchangeTicket) exchangeTicketData.ExchangeTicket = { Exchange: [] };
    if (!exchangeTicketData.ExchangeTicket.Exchange) exchangeTicketData.ExchangeTicket.Exchange = [];

    gatherExchangeTicketData();
    exchangeTicketData.ExchangeTicket.Exchange.push({
        $: { TimeKey: "New_Exchange_Key", Name: "Tên Event Mới", RateKNB: "100", ItemID: "0", ItemCount: "1", Message: "Mô tả nhận quà" }
    });
    renderExchangeTicketForm(exchangeTicketData);
}

function deleteExchangeTicketConfig(idx) {
    if (confirm('Bạn có chắc muốn xóa cấu hình này?')) {
        gatherExchangeTicketData();
        exchangeTicketData.ExchangeTicket.Exchange.splice(idx, 1);
        renderExchangeTicketForm(exchangeTicketData);
    }
}

async function saveExchangeTicket() {
    if (!exchangeTicketData) return;
    gatherExchangeTicketData();
    exchangeTicketData.isExport = getExportState();
    try {
        const response = await fetch('/api/exchange-ticket', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(exchangeTicketData)
        });
        if (response.ok) showAlert('Lưu cấu hình Chuyển Xu thành công!', 'success');
        else throw new Error(await response.text());
    } catch (err) { showAlert('Lỗi: ' + err.message, 'danger'); }
}

// ----------------------------------------------------
// EVENT COMPOSE (EventCompose.xml)
// ----------------------------------------------------
let eventComposeXmlData = null;

async function loadComposeXml() {
    try {
        const response = await fetch('/api/event-compose-xml');
        if (!response.ok) throw new Error('Failed to fetch EventCompose.xml data');
        eventComposeXmlData = await response.json();
        renderComposeXmlForm(eventComposeXmlData);
    } catch (error) {
        document.getElementById('compose-xml-form').innerHTML = '<p class="text-danger">Lỗi tải dữ liệu.</p>';
    }
}

function renderComposeXmlForm(data) {
    const container = document.getElementById('compose-xml-form');
    if (!data.ComposeData || !data.ComposeData.Compose) return;

    const rootAttr = data.ComposeData.$ || {};
    const mantleAttr = data.ComposeData.MantleRequirement ? (data.ComposeData.MantleRequirement[0].$ || {}) : {};

    let html = `
        <div class="card mb-4 border-primary shadow-sm">
            <div class="card-header bg-primary text-white py-2">
                <h6 class="fw-bold mb-0"><i class="bi bi-gear"></i> Thông số cấu hình chung Event Ghép</h6>
            </div>
            <div class="card-body">
                <div class="row g-3">
                    <div class="col-md-2">
                        <label class="form-label small fw-bold">Túi mù (MainItem)</label>
                        <input type="number" class="form-control form-control-sm cmp-root-attr" data-attr="MainItem" value="${rootAttr.MainItem || ''}">
                    </div>
                    <div class="col-md-2">
                        <label class="form-label small fw-bold">Thưởng Bonus (Item)</label>
                        <input type="number" class="form-control form-control-sm cmp-root-attr" data-attr="BonusItem" value="${rootAttr.BonusItem || ''}">
                    </div>
                    <div class="col-md-2">
                        <label class="form-label small fw-bold">Mốc Bonus (Step)</label>
                        <input type="number" class="form-control form-control-sm cmp-root-attr" data-attr="BonusStep" value="${rootAttr.BonusStep || ''}">
                    </div>
                    <div class="col-md-2">
                        <label class="form-label small fw-bold">Max Bonus Ngày</label>
                        <input type="number" class="form-control form-control-sm cmp-root-attr" data-attr="BonusMaxDaily" value="${rootAttr.BonusMaxDaily || ''}">
                    </div>
                    <div class="col-md-4">
                        <label class="form-label small fw-bold">RecordSeed (Mã hạt giống record)</label>
                        <input type="number" class="form-control form-control-sm cmp-root-attr" data-attr="RecordSeed" value="${rootAttr.RecordSeed || ''}" placeholder="Số ID record">
                    </div>
                    
                    <div class="col-md-12">
                        <label class="form-label small fw-bold">Danh sách vật phẩm rớt ra (DropItem)</label>
                        <input type="text" class="form-control form-control-sm cmp-root-attr" data-attr="DropItem" value="${rootAttr.DropItem || ''}" placeholder="Vd: 20482,20483,20484,20485">
                    </div>
                </div>
            </div>
        </div>

        <!-- Mantle Requirements Section -->
        <div class="card mb-4 border-info shadow-sm">
            <div class="card-header bg-info text-white py-1 d-flex justify-content-between align-items-center">
                <h6 class="fw-bold mb-0"><i class="bi bi-shield-shaded"></i> Điều kiện Phi Phong (Mantle Requirement)</h6>
                <button class="btn btn-xs btn-light" onclick="addComposeXmlMantle()"><i class="bi bi-plus-circle"></i> Thêm mốc</button>
            </div>
            <div class="card-body p-0">
                <table class="table table-sm table-hover mb-0">
                    <thead class="table-light small">
                        <tr>
                            <th class="px-3">Cấp Phi Phong</th>
                            <th>Server Min</th>
                            <th>Server Max</th>
                            <th width="40"></th>
                        </tr>
                    </thead>
                    <tbody>
    `;

    if (data.ComposeData.MantleRequirement) {
        data.ComposeData.MantleRequirement.forEach((mantle, midx) => {
            const mAttr = mantle.$ || {};
            html += `
                <tr>
                    <td class="px-3"><input type="number" class="form-control form-control-sm cmp-mantle-attr" data-midx="${midx}" data-attr="Level" value="${mAttr.Level || ''}"></td>
                    <td><input type="number" class="form-control form-control-sm cmp-mantle-attr" data-midx="${midx}" data-attr="ServerMin" value="${mAttr.ServerMin || ''}"></td>
                    <td><input type="number" class="form-control form-control-sm cmp-mantle-attr" data-midx="${midx}" data-attr="ServerMax" value="${mAttr.ServerMax || ''}"></td>
                    <td class="text-center"><button class="btn btn-link text-danger p-0" onclick="deleteComposeXmlMantle(${midx})"><i class="bi bi-trash"></i></button></td>
                </tr>
            `;
        });
    } else {
        html += `<tr><td colspan="4" class="text-center text-muted py-2 small">Chưa có điều kiện phi phong nào</td></tr>`;
    }

    html += `
                    </tbody>
                </table>
            </div>
        </div>
    `;

    data.ComposeData.Compose.forEach((cmp, cIdx) => {
        const cAttr = cmp.$ || {};
        html += `<div class="card mb-2 border-secondary" data-group="${cAttr.Group || '0'}" data-limittype="${cAttr.LimitType || 'None'}">
            <div class="card-header bg-light d-flex justify-content-between align-items-center py-1">
                <span style="font-size: 0.9rem;">
                    <b>Vật phẩm nhận:</b> <input type="number" class="form-control d-inline w-auto form-control-sm cx-attr" data-cidx="${cIdx}" data-attr="Output" value="${cAttr.Output || ''}" style="width: 80px!important">
                    <b>SL:</b> <input type="number" class="form-control d-inline w-auto form-control-sm cx-attr" data-cidx="${cIdx}" data-attr="OutputNumber" value="${cAttr.OutputNumber || ''}" style="width: 60px!important">
                    <b>Loại giới hạn:</b> 
                    <select class="form-select d-inline w-auto form-select-sm cx-attr" data-cidx="${cIdx}" data-attr="LimitType" style="width: 100px!important">
                        <option value="None" ${cAttr.LimitType === 'None' ? 'selected' : ''}>None</option>
                        <option value="Day" ${cAttr.LimitType === 'Day' ? 'selected' : ''}>Day</option>
                        <option value="Week" ${cAttr.LimitType === 'Week' ? 'selected' : ''}>Week</option>
                        <option value="Month" ${cAttr.LimitType === 'Month' ? 'selected' : ''}>Month</option>
                        <option value="Lifetime" ${cAttr.LimitType === 'Lifetime' ? 'selected' : ''}>Lifetime</option>
                    </select>
                    <b>Số lần:</b> <input type="number" class="form-control d-inline w-auto form-control-sm cx-attr" data-cidx="${cIdx}" data-attr="LimitValue" value="${cAttr.LimitValue || ''}" style="width: 65px!important">
                    <b>Nhóm:</b> 
                    <select class="form-select d-inline w-auto form-select-sm cx-attr" data-cidx="${cIdx}" data-attr="Group" style="width: 110px!important">
                        <option value="0" ${cAttr.Group == '0' ? 'selected' : ''}>Nông dân</option>
                        <option value="1" ${cAttr.Group == '1' ? 'selected' : ''}>Năng động</option>
                        <option value="2" ${cAttr.Group == '2' ? 'selected' : ''}>Sự kiện</option>
                        <option value="5" ${cAttr.Group == '5' ? 'selected' : ''}>Du Long</option>
                    </select>
                </span>
                <div>
                   <button class="btn btn-sm btn-outline-primary py-0" onclick="addComposeXmlMaterial(${cIdx})">+Nguyên liệu</button>
                   <button class="btn btn-sm btn-outline-warning py-0" onclick="addComposeXmlFee(${cIdx})">+Phí</button>
                   <button class="btn btn-sm btn-outline-danger py-0" onclick="deleteComposeXmlRecipe(${cIdx})"><i class="bi bi-trash"></i></button>
                </div>
            </div>
            <div class="card-body py-1">
                <div class="row">`;

        // Materials
        html += `<div class="col-md-6 border-end"><h6>Nguyên liệu (Materials)</h6><ul class="list-unstyled mb-0">`;
        if (cmp.Material) {
            cmp.Material.forEach((mat, mIdx) => {
                const mAttr = mat.$ || {};
                html += `<li class="d-flex mb-1 gap-1 align-items-center"><small>ID:</small> <input type="number" class="form-control form-control-sm cx-mat-attr" data-cidx="${cIdx}" data-midx="${mIdx}" data-attr="ID" value="${mAttr.ID || mAttr.id || ''}"> 
                <small>SL:</small> <input type="number" class="form-control form-control-sm cx-mat-attr" data-cidx="${cIdx}" data-midx="${mIdx}" data-attr="Number" value="${mAttr.Number || ''}">
                <button class="btn btn-xs text-danger p-0" onclick="deleteComposeXmlMaterial(${cIdx}, ${mIdx})"><i class="bi bi-x"></i></button></li>`;
            });
        }
        html += `</ul></div>`;

        // Fees
        html += `<div class="col-md-6"><h6>Chi phí (Fees)</h6><ul class="list-unstyled mb-0">`;
        if (cmp.Fee) {
            cmp.Fee.forEach((fee, fIdx) => {
                const fAttr = fee.$ || {};
                const currentFeeId = fAttr.ID || fAttr.id || '1';
                html += `<li class="d-flex mb-1 gap-1 align-items-center">
                <small>Loại:</small> 
                <select class="form-select form-select-sm cx-fee-attr" data-cidx="${cIdx}" data-fidx="${fIdx}" data-attr="ID" style="width: 110px!important">
                    <option value="0" ${currentFeeId == '0' ? 'selected' : ''}>Bạc Khóa</option>
                    <option value="1" ${currentFeeId == '1' ? 'selected' : ''}>Bạc</option>
                    <option value="2" ${currentFeeId == '2' ? 'selected' : ''}>KNB</option>
                    <option value="3" ${currentFeeId == '3' ? 'selected' : ''}>KNB Khóa</option>
                </select>
                <small>SL:</small> <input type="number" class="form-control form-control-sm cx-fee-attr" data-cidx="${cIdx}" data-fidx="${fIdx}" data-attr="Number" value="${fAttr.Number || ''}">
                <button class="btn btn-xs text-danger p-0" onclick="deleteComposeXmlFee(${cIdx}, ${fIdx})"><i class="bi bi-x"></i></button></li>`;
            });
        }
        html += `</ul></div></div></div></div>`;
    });
    container.innerHTML = html;

    // Cập nhật bộ đếm công thức
    const countEl = document.getElementById('compose-filter-count');
    if (countEl) countEl.textContent = `${data.ComposeData.Compose.length} công thức`;
}

// Lọc công thức theo Group, LimitType, và text
function filterComposeXml() {
    const q = (document.getElementById('search-compose-xml')?.value || '').toLowerCase().trim();
    const group = document.getElementById('filter-compose-group')?.value || '';
    const limitType = document.getElementById('filter-compose-limit')?.value || '';

    const cards = document.querySelectorAll('#compose-xml-form .card[data-group]');
    let visibleCount = 0;

    cards.forEach(card => {
        const cardGroup = card.dataset.group || '';
        const cardLimit = card.dataset.limittype || '';

        // Kiểm tra Group
        const matchGroup = !group || cardGroup === group;
        // Kiểm tra LimitType
        const matchLimit = !limitType || cardLimit === limitType;
        // Kiểm tra text (dùng getSearchableText)
        const matchText = !q || getSearchableText(card).includes(q);

        const visible = matchGroup && matchLimit && matchText;
        card.style.display = visible ? '' : 'none';
        if (visible) visibleCount++;
    });

    // Cập nhật bộ đếm
    const countEl = document.getElementById('compose-filter-count');
    if (countEl) {
        countEl.textContent = `${visibleCount} công thức`;
        countEl.className = `badge fs-6 px-3 ${visibleCount === 0 ? 'bg-danger' : 'bg-primary'}`;
    }
}

// Xóa bộ lọc công thức
function resetComposeFilter() {
    const searchEl = document.getElementById('search-compose-xml');
    const groupEl = document.getElementById('filter-compose-group');
    const limitEl = document.getElementById('filter-compose-limit');
    if (searchEl) searchEl.value = '';
    if (groupEl) groupEl.value = '';
    if (limitEl) limitEl.value = '';
    filterComposeXml();
}

/**
 * Replace nhanh một field trên tất cả công thức đang HIỂN THỊ (có thể đã lọc).
 * Áp dụng trực tiếp vào eventComposeXmlData rồi re-render.
 */
function replaceComposeXmlField() {
    if (!eventComposeXmlData) return;

    const field = document.getElementById('replace-compose-field')?.value;
    const fromVal = document.getElementById('replace-compose-from')?.value.trim();
    const toVal = document.getElementById('replace-compose-to')?.value.trim();
    const resultEl = document.getElementById('replace-compose-result');

    if (!fromVal || !toVal || !field) {
        if (resultEl) resultEl.innerHTML = '<span class="text-danger">Vui lòng điền đủ field, giá trị cũ và mới.</span>';
        return;
    }

    // Thu thập data từ DOM trước khi replace
    gatherComposeXmlData();

    // Xác định các index công thức đang hiển thị
    const visibleCards = document.querySelectorAll('#compose-xml-form .card[data-group]:not([style*="display: none"])');
    const visibleIndices = new Set();
    visibleCards.forEach(card => {
        // Tìm index từ cx-attr bên trong card
        const firstAttr = card.querySelector('[data-cidx]');
        if (firstAttr) visibleIndices.add(parseInt(firstAttr.dataset.cidx));
    });

    let count = 0;
    const composes = eventComposeXmlData.ComposeData.Compose;

    composes.forEach((cmp, cIdx) => {
        if (!visibleIndices.has(cIdx)) return; // Bỏ qua công thức bị ẩn

        const cAttr = cmp.$ || {};

        if (['Output', 'OutputNumber', 'LimitValue', 'LimitType', 'Group'].includes(field)) {
            // Thay thế field trực tiếp trên Compose
            if (String(cAttr[field]) === fromVal) {
                cAttr[field] = toVal;
                count++;
            }
        } else if (field === 'material_id') {
            // Thay ID nguyên liệu
            (cmp.Material || []).forEach(mat => {
                const key = 'ID' in mat.$ ? 'ID' : 'id';
                if (String(mat.$[key]) === fromVal) { mat.$[key] = toVal; count++; }
            });
        } else if (field === 'material_number') {
            // Thay số lượng nguyên liệu
            (cmp.Material || []).forEach(mat => {
                if (String(mat.$.Number) === fromVal) { mat.$.Number = toVal; count++; }
            });
        } else if (field === 'fee_id') {
            // Thay loại phí
            (cmp.Fee || []).forEach(fee => {
                const key = 'ID' in fee.$ ? 'ID' : 'id';
                if (String(fee.$[key]) === fromVal) { fee.$[key] = toVal; count++; }
            });
        } else if (field === 'fee_number') {
            // Thay số lượng phí
            (cmp.Fee || []).forEach(fee => {
                if (String(fee.$.Number) === fromVal) { fee.$.Number = toVal; count++; }
            });
        }
    });

    // Re-render và hiển thị kết quả
    renderComposeXmlForm(eventComposeXmlData);

    // Giữ lại bộ lọc sau khi re-render
    filterComposeXml();

    if (resultEl) {
        if (count > 0) {
            resultEl.innerHTML = `<span class="text-success fw-bold"><i class="bi bi-check-circle-fill"></i> Đã thay ${count} trường hợp.</span>`;
        } else {
            resultEl.innerHTML = `<span class="text-warning"><i class="bi bi-exclamation-triangle"></i> Không tìm thấy giá trị "${fromVal}" nào.</span>`;
        }
    }
}

function gatherComposeXmlData() {
    document.querySelectorAll('.cmp-root-attr').forEach(input => {
        if (!eventComposeXmlData.ComposeData.$) eventComposeXmlData.ComposeData.$ = {};
        eventComposeXmlData.ComposeData.$[input.dataset.attr] = input.value;
    });
    document.querySelectorAll('.cmp-mantle-attr').forEach(input => {
        const midx = input.dataset.midx;
        if (!eventComposeXmlData.ComposeData.MantleRequirement[midx].$) eventComposeXmlData.ComposeData.MantleRequirement[midx].$ = {};
        eventComposeXmlData.ComposeData.MantleRequirement[midx].$[input.dataset.attr] = input.value;
    });

    document.querySelectorAll('.cx-attr').forEach(input => {
        eventComposeXmlData.ComposeData.Compose[input.dataset.cidx].$[input.dataset.attr] = input.value;
    });
    document.querySelectorAll('.cx-mat-attr').forEach(input => {
        const matNode = eventComposeXmlData.ComposeData.Compose[input.dataset.cidx].Material[input.dataset.midx].$;
        if (input.dataset.attr === "ID") {
            if ("ID" in matNode) matNode.ID = input.value;
            else matNode.id = input.value;
        } else {
            matNode[input.dataset.attr] = input.value;
        }
    });
    document.querySelectorAll('.cx-fee-attr').forEach(input => {
        const feeNode = eventComposeXmlData.ComposeData.Compose[input.dataset.cidx].Fee[input.dataset.fidx].$;
        if (input.dataset.attr === "ID") {
            if ("ID" in feeNode) feeNode.ID = input.value;
            else feeNode.id = input.value;
        } else {
            feeNode[input.dataset.attr] = input.value;
        }
    });
}

function addComposeXmlRecipe() {
    gatherComposeXmlData();
    if (!eventComposeXmlData.ComposeData.Compose) eventComposeXmlData.ComposeData.Compose = [];
    eventComposeXmlData.ComposeData.Compose.push({
        $: { Output: "0", OutputNumber: "1", LimitType: "None", LimitValue: "0", Group: "0" },
        Material: [], Fee: []
    });
    renderComposeXmlForm(eventComposeXmlData);
}

function deleteComposeXmlRecipe(cIdx) {
    gatherComposeXmlData();
    eventComposeXmlData.ComposeData.Compose.splice(cIdx, 1);
    renderComposeXmlForm(eventComposeXmlData);
}

function addComposeXmlMaterial(cIdx) {
    gatherComposeXmlData();
    if (!eventComposeXmlData.ComposeData.Compose[cIdx].Material) eventComposeXmlData.ComposeData.Compose[cIdx].Material = [];
    eventComposeXmlData.ComposeData.Compose[cIdx].Material.push({ $: { ID: "1", Number: "1" } });
    renderComposeXmlForm(eventComposeXmlData);
}

function deleteComposeXmlMaterial(cIdx, mIdx) {
    gatherComposeXmlData();
    eventComposeXmlData.ComposeData.Compose[cIdx].Material.splice(mIdx, 1);
    renderComposeXmlForm(eventComposeXmlData);
}

function addComposeXmlFee(cIdx) {
    gatherComposeXmlData();
    if (!eventComposeXmlData.ComposeData.Compose[cIdx].Fee) eventComposeXmlData.ComposeData.Compose[cIdx].Fee = [];
    eventComposeXmlData.ComposeData.Compose[cIdx].Fee.push({ $: { ID: "1", Number: "1000" } });
    renderComposeXmlForm(eventComposeXmlData);
}

function deleteComposeXmlFee(cIdx, fIdx) {
    gatherComposeXmlData();
    eventComposeXmlData.ComposeData.Compose[cIdx].Fee.splice(fIdx, 1);
    renderComposeXmlForm(eventComposeXmlData);
}

async function saveComposeXml() {
    if (!eventComposeXmlData) return;
    gatherComposeXmlData();
    eventComposeXmlData.isExport = getExportState();
    try {
        const response = await fetch('/api/event-compose-xml', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(eventComposeXmlData)
        });
        if (response.ok) showAlert('Lưu Công thức Compose thành công!', 'success');
        else throw new Error(await response.text());
    } catch (err) { showAlert('Lỗi: ' + err.message, 'danger'); }
}

function addComposeXmlMantle() {
    gatherComposeXmlData();
    if (!eventComposeXmlData.ComposeData.MantleRequirement) eventComposeXmlData.ComposeData.MantleRequirement = [];
    eventComposeXmlData.ComposeData.MantleRequirement.push({
        $: { Level: "6", ServerMin: "1", ServerMax: "999" }
    });
    renderComposeXmlForm(eventComposeXmlData);
}

function deleteComposeXmlMantle(midx) {
    gatherComposeXmlData();
    eventComposeXmlData.ComposeData.MantleRequirement.splice(midx, 1);
    renderComposeXmlForm(eventComposeXmlData);
}

// ----------------------------------------------------
// UU DAI & TICH LUY (RechargeAward.xml & UuDaiDisplay.xml)
// ----------------------------------------------------

async function loadRechargeAward() {
    // Merged into loadRechargeManagement, but keep function if needed by other parts
    // For now, it will just re-render if data already exists
    if (rechargeAwardData) renderRechargeAwardForm(rechargeAwardData);
}

function renderRechargeAwardSection(containerId, sectionData, cName, prefix) {
    const container = document.getElementById(containerId);
    if (!sectionData || !sectionData[0].RechargeAward) return;

    let html = `<div class="table-responsive">
        <table class="table table-sm table-bordered table-hover align-middle mb-0">
            <thead class="table-light small">
                <tr>
                    <th width="80">ID</th>
                    <th>KNB</th>
                    <th>Quà nạp (ItemBox)</th>
                    <th>Thưởng tích lũy (BonusItem)</th>
                    <th width="50"></th>
                </tr>
            </thead>
            <tbody>`;

    sectionData[0].RechargeAward.forEach((item, idx) => {
        const attr = item.$ || {};
        html += `<tr>
            <td><input type="number" class="form-control form-control-sm ${prefix}-attr" data-idx="${idx}" data-attr="ID" value="${attr.ID}"></td>
            <td><input type="number" class="form-control form-control-sm ${prefix}-attr" data-idx="${idx}" data-attr="KNB" value="${attr.KNB}"></td>
            <td><input type="number" class="form-control form-control-sm ${prefix}-attr" data-idx="${idx}" data-attr="ItemBox" value="${attr.ItemBox}"></td>
            <td><input type="number" class="form-control form-control-sm ${prefix}-attr" data-idx="${idx}" data-attr="BonusItem" value="${attr.BonusItem}"></td>
            <td class="text-center"><button class="btn btn-sm btn-outline-danger py-0 px-1" onclick="delete${cName}(${idx})"><i class="bi bi-trash"></i></button></td>
        </tr>`;
    });

    html += `</tbody></table></div>`;
    container.innerHTML = html;
}

function renderRechargeAwardForm(data) {
    if (!data.RechargeAward) return;
    if (data.RechargeAward.DayRecharge) renderRechargeAwardSection('uudai-dayrecharge-form', data.RechargeAward.DayRecharge, 'DayRecharge', 'dr');
    if (data.RechargeAward.AccumulateRecharge) renderRechargeAwardSection('uudai-accrecharge-form', data.RechargeAward.AccumulateRecharge, 'AccRecharge', 'ar');
}

function gatherRechargeAwardData() {
    document.querySelectorAll('.dr-attr').forEach(input => {
        rechargeAwardData.RechargeAward.DayRecharge[0].RechargeAward[input.dataset.idx].$[input.dataset.attr] = input.value;
    });
    document.querySelectorAll('.ar-attr').forEach(input => {
        rechargeAwardData.RechargeAward.AccumulateRecharge[0].RechargeAward[input.dataset.idx].$[input.dataset.attr] = input.value;
    });
}

function addDayRecharge() {
    gatherRechargeAwardData();
    if (!rechargeAwardData.RechargeAward.DayRecharge) { rechargeAwardData.RechargeAward.DayRecharge = [{ RechargeAward: [] }]; }
    rechargeAwardData.RechargeAward.DayRecharge[0].RechargeAward.push({ $: { ID: "0", KNB: "100", ItemBox: "1", BonusItem: "1" } });
    renderRechargeAwardForm(rechargeAwardData);
}
function deleteDayRecharge(idx) {
    gatherRechargeAwardData();
    rechargeAwardData.RechargeAward.DayRecharge[0].RechargeAward.splice(idx, 1);
    renderRechargeAwardForm(rechargeAwardData);
}
function addAccRecharge() {
    gatherRechargeAwardData();
    if (!rechargeAwardData.RechargeAward.AccumulateRecharge) { rechargeAwardData.RechargeAward.AccumulateRecharge = [{ RechargeAward: [] }]; }
    rechargeAwardData.RechargeAward.AccumulateRecharge[0].RechargeAward.push({ $: { ID: "0", KNB: "100", ItemBox: "1", BonusItem: "1" } });
    renderRechargeAwardForm(rechargeAwardData);
}
function deleteAccRecharge(idx) {
    gatherRechargeAwardData();
    rechargeAwardData.RechargeAward.AccumulateRecharge[0].RechargeAward.splice(idx, 1);
    renderRechargeAwardForm(rechargeAwardData);
}

async function saveRechargeAward() {
    if (!rechargeAwardData) return;
    gatherRechargeAwardData();
    rechargeAwardData.isExport = getExportState();
    try {
        const response = await fetch('/api/recharge-award', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(rechargeAwardData)
        });
        if (response.ok) showAlert('Lưu Cấu Hình Nạp Đơn & Tích Lũy thành công!', 'success');
        else throw new Error(await response.text());
    } catch (err) { showAlert('Lỗi: ' + err.message, 'danger'); }
}

// UuDai Display
let uudaiDisplayData = null;

function changeUuDaiType(type) {
    currentUuDaiType = type;
    const fileName = type === 'normal' ? 'UuDaiDisplay.xml' : 'UuDaiDisplay_NewServer.xml';
    const label = document.getElementById('uudai-current-file');
    if (label) label.innerText = `(Đang sửa: ${fileName})`;
    loadUuDaiDisplay();
}

async function loadUuDaiDisplay() {
    try {
        const endpoint = currentUuDaiType === 'normal' ? '/api/uudai-display' : '/api/uudai-display-newserver';
        const response = await fetch(endpoint);
        if (!response.ok) throw new Error('Failed to fetch data');
        uudaiDisplayData = await response.json();
        renderUuDaiDisplayForm(uudaiDisplayData);
    } catch (error) {
        console.error("UuDai load error:", error);
        showAlert('Lỗi khi tải dữ liệu Ưu đãi.', 'danger');
    }
}

function renderUuDaiDisplayForm(data) {
    if (!data.UuDaiDisplay) return;
    
    const allRewards = data.UuDaiDisplay.Reward || [];
    
    // Phân loại dựa trên thuộc tính Group
    // Group 0: Hàng ngày
    // Group 1: Giới hạn
    // Group 2: Tài Nguyên
    const rewardsDaily = allRewards.map((r, i) => ({...r, globalIdx: i})).filter(r => (r.$ && r.$.Group == "0") || !r.$ || !r.$.Group);
    const rewardsLimit = allRewards.map((r, i) => ({...r, globalIdx: i})).filter(r => r.$ && r.$.Group == "1");
    const rewardsResource = allRewards.map((r, i) => ({...r, globalIdx: i})).filter(r => r.$ && r.$.Group == "2");

    renderUuDaiSection('uudai-reward-daily-form', rewardsDaily, 'ud-0');
    renderUuDaiSection('uudai-reward-limit-form', rewardsLimit, 'ud-1');
    renderUuDaiSection('uudai-reward-resource-form', rewardsResource, 'ud-2');
}

function renderUuDaiSection(containerId, list, prefix) {
    const container = document.getElementById(containerId);
    if (!list) return;

    let html = `<div class="list-group list-group-flush border rounded" style="max-height: 400px; overflow-y: auto;">`;
    list.forEach((rw) => {
        const rAttr = rw.$ || {};
        const rIdx = rw.globalIdx; // Sử dụng index toàn cục để chỉnh sửa đúng phần tử trong mảng gốc
        html += `
            <div class="list-group-item d-flex justify-content-between align-items-center py-1 px-2 border-bottom">
                <span class="small fw-bold">${rAttr.Name || 'Không tên'} <span class="text-muted">(L:${rAttr.Limit})</span></span>
                <div class="btn-group">
                    <button class="btn btn-xs btn-outline-primary py-0" onclick="openUuDaiEditor('${prefix}', ${rIdx})"><i class="bi bi-pencil-square"></i></button>
                    <button class="btn btn-xs btn-outline-danger py-0" onclick="deleteUuDaiItem(${rIdx})"><i class="bi bi-trash"></i></button>
                </div>
            </div>`;
    });

    if (!list || list.length === 0) {
        html = '<p class="text-muted p-3 mb-0">Chưa có dữ liệu.</p>';
    } else {
        html += '</div>';
    }
    container.innerHTML = html;
}

function openUuDaiEditor(prefix, globalIdx) {
    const rw = uudaiDisplayData.UuDaiDisplay.Reward[globalIdx];
    const attr = rw.$ || {};

    document.getElementById('edit-uudai-type').value = prefix;
    document.getElementById('edit-uudai-idx').value = globalIdx;
    document.getElementById('edit-uudai-name').value = attr.Name || '';
    document.getElementById('edit-uudai-limit').value = attr.Limit || 0;
    document.getElementById('edit-uudai-limittype').value = attr.LimitType || '';

    renderUuDaiEditorItemsTable(rw.Item || []);

    document.getElementById('uudaiEditorModalLabel').innerText = `Sửa Gói: ${attr.Name || 'Mới'}`;
    const modal = new bootstrap.Modal(document.getElementById('uudaiEditorModal'));
    modal.show();
}

function renderUuDaiEditorItemsTable(items) {
    const tbody = document.getElementById('uudai-editor-items-tbody');
    let html = '';
    items.forEach((item, idx) => {
        const iAttr = item.$ || {};
        html += `
            <tr>
                <td><input type="number" class="form-control form-control-sm ud-item-attr" data-idx="${idx}" data-attr="Id" value="${iAttr.Id || iAttr.ID}"></td>
                <td><input type="number" class="form-control form-control-sm ud-item-attr" data-idx="${idx}" data-attr="Count" value="${iAttr.Count}"></td>
                <td class="text-center">
                    <button class="btn btn-sm btn-outline-danger" onclick="deleteUuDaiEditorItemRow(${idx})"><i class="bi bi-trash"></i></button>
                </td>
            </tr>`;
    });
    tbody.innerHTML = html;
}

function addUuDaiEditorItem() {
    const globalIdx = document.getElementById('edit-uudai-idx').value;
    const rw = uudaiDisplayData.UuDaiDisplay.Reward[globalIdx];

    if (!rw.Item) rw.Item = [];
    rw.Item.push({ $: { Id: "1", Count: "1" } });
    renderUuDaiEditorItemsTable(rw.Item);
}

function deleteUuDaiEditorItemRow(itemIdx) {
    const globalIdx = document.getElementById('edit-uudai-idx').value;
    const rw = uudaiDisplayData.UuDaiDisplay.Reward[globalIdx];

    rw.Item.splice(itemIdx, 1);
    renderUuDaiEditorItemsTable(rw.Item);
}

function saveUuDaiEditorChanges() {
    const prefix = document.getElementById('edit-uudai-type').value;
    const globalIdx = document.getElementById('edit-uudai-idx').value;
    const rw = uudaiDisplayData.UuDaiDisplay.Reward[globalIdx];

    let groupVal = "0";
    if (prefix === 'ud-1') groupVal = "1";
    if (prefix === 'ud-2') groupVal = "2";

    rw.$ = {
        Name: document.getElementById('edit-uudai-name').value,
        Limit: document.getElementById('edit-uudai-limit').value,
        LimitType: document.getElementById('edit-uudai-limittype').value,
        Group: groupVal
    };

    // Gather Items
    const rows = document.querySelectorAll('.ud-item-attr');
    rows.forEach(input => {
        const iIdx = input.dataset.idx;
        const attr = input.dataset.attr;
        if (!rw.Item[iIdx].$) rw.Item[iIdx].$ = {};
        rw.Item[iIdx].$[attr] = input.value;
    });

    renderUuDaiDisplayForm(uudaiDisplayData);
    const modalEl = document.getElementById('uudaiEditorModal');
    const modal = bootstrap.Modal.getInstance(modalEl);
    if (modal) modal.hide();
}

function addUuDaiReward(group) {
    if (!uudaiDisplayData.UuDaiDisplay.Reward) uudaiDisplayData.UuDaiDisplay.Reward = [];
    const newItem = { $: { Name: "Gói Mới", Limit: "1", LimitType: "ngày", Group: group }, Item: [] };
    uudaiDisplayData.UuDaiDisplay.Reward.push(newItem);
    renderUuDaiDisplayForm(uudaiDisplayData);
    openUuDaiEditor('ud-' + group, uudaiDisplayData.UuDaiDisplay.Reward.length - 1);
}

function deleteUuDaiItem(globalIdx) {
    if (confirm(`Bạn có chắc muốn xóa gói này?`)) {
        uudaiDisplayData.UuDaiDisplay.Reward.splice(globalIdx, 1);
        renderUuDaiDisplayForm(uudaiDisplayData);
    }
}

async function saveUuDaiDisplay() {
    if (!uudaiDisplayData) return;
    uudaiDisplayData.isExport = getExportState();
    try {
        const endpoint = currentUuDaiType === 'normal' ? '/api/uudai-display' : '/api/uudai-display-newserver';
        const response = await fetch(endpoint, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(uudaiDisplayData)
        });
        if (response.ok) showAlert(`Lưu Giao diện Ưu Đãi (${currentUuDaiType}) thành công!`, 'success');
        else throw new Error(await response.text());
    } catch (err) { showAlert('Lỗi: ' + err.message, 'danger'); }
}
// ==========================================
// BOX EDITOR LOGIC
// ==========================================
// Data is now loaded via loadRechargeManagement in DOMContentLoaded

function openBoxEditorFromModal() {
    const boxId = document.getElementById('edit-script-item-value').value;
    if (!boxId || boxId == 0) {
        alert("Vui lòng nhập Box ID hợp lệ.");
        return;
    }

    // Try to detect the correct file type
    let detectedType = 'DayRecharge'; // Default
    const isMilestone = randomboxesMilestones.some(b => Number(b.IDBox) === Number(boxId));
    if (isMilestone) {
        detectedType = 'AccRecharge';
    } else {
        const isPackage = randomboxesPackages.some(b => Number(b.IDBox) === Number(boxId));
        if (isPackage) detectedType = 'DayRecharge';
        else {
            alert(`Không tìm thấy Box ID ${boxId} trong cả file Packages và Milestones.`);
            return;
        }
    }

    // Hide the script item modal first to avoid background stacking issues
    const scriptItemModalEl = document.getElementById('scriptItemModal');
    const scriptItemModal = bootstrap.Modal.getInstance(scriptItemModalEl);
    if (scriptItemModal) scriptItemModal.hide();

    // Open box editor
    openBoxEditor(boxId, detectedType, true);

    // When box editor hides, show script item modal again
    const boxEditorModalEl = document.getElementById('boxEditorModal');
    const onBoxModalHidden = () => {
        if (scriptItemModal) scriptItemModal.show();
        boxEditorModalEl.removeEventListener('hidden.bs.modal', onBoxModalHidden);
    };
    boxEditorModalEl.addEventListener('hidden.bs.modal', onBoxModalHidden);
}

function openBoxEditor(id, type, isDirect = false) {
    const alertEl = document.getElementById('boxEditorAlert');
    alertEl.innerHTML = `Đang tải Box ID: <b>${id}</b>...`;

    let boxList = [];
    if (type === 'DayRecharge') boxList = randomboxesPackages;
    else if (type === 'AccRecharge') boxList = randomboxesMilestones;
    else if (type === 'FlowerTower') boxList = randomboxesFlowerTower;
    else if (type === 'General') boxList = randomboxesGeneral;

    const box = boxList.find(b => Number(b.IDBox) === Number(id));

    if (!box) {
        alertEl.innerHTML = `<span class="text-danger">Không tìm thấy Box ID ${id} trong dữ liệu (${type}).</span>`;
        return;
    }

    currentEditingBox = { id: id, type: type, data: box };
    document.getElementById('edit-box-name').value = box.BoxName || '';
    document.getElementById('edit-box-comment').value = box._comment || '';

    renderBoxEditorTable(box.Items);

    const modalMod = new bootstrap.Modal(document.getElementById('boxEditorModal'));
    modalMod.show();
}

function renderBoxEditorTable(items) {
    const tbody = document.getElementById('boxEditorTbody');
    if (!items || items.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" class="text-center text-muted">Box trống chưa có phần thưởng</td></tr>';
        return;
    }

    let html = '';
    items.forEach((item, idx) => {
        html += `<tr>
            <td><input type="number" class="form-control form-control-sm be-attr" data-idx="${idx}" data-attr="ItemID" value="${item.ItemID}"></td>
            <td><input type="number" class="form-control form-control-sm be-attr" data-idx="${idx}" data-attr="Number" value="${item.Number}"></td>
            <td><input type="number" class="form-control form-control-sm be-attr" data-idx="${idx}" data-attr="Rate" value="${item.Rate || -1}"></td>
            <td><input type="text" class="form-control form-control-sm be-attr" data-idx="${idx}" data-attr="Info" value="${item.Info || ''}"></td>
            <td><select class="form-select form-select-sm be-attr" data-idx="${idx}" data-attr="IsPreview">
                <option value="true" ${item.IsPreview ? 'selected' : ''}>True</option>
                <option value="false" ${!item.IsPreview ? 'selected' : ''}>False</option>
            </select></td>
            <td class="text-center"><button class="btn btn-sm btn-outline-danger" onclick="deleteBoxItemRow(${idx})"><i class="bi bi-trash"></i></button></td>
        </tr>`;
    });
    tbody.innerHTML = html;
}

function gatherBoxEditorData(box) {
    if (!box) return;
    box.BoxName = document.getElementById('edit-box-name').value;
    box._comment = document.getElementById('edit-box-comment').value;

    if (!box.Items) return;
    document.querySelectorAll('.be-attr').forEach(input => {
        const idx = input.dataset.idx;
        const attr = input.dataset.attr;
        let val = input.value;
        if (input.type === 'number') val = Number(val);
        else if (val === 'true') val = true;
        else if (val === 'false') val = false;
        box.Items[idx][attr] = val;
    });
}

function addBoxItemRow() {
    if (!currentEditingBox || !currentEditingBox.data) return;
    gatherBoxEditorData(currentEditingBox.data);
    if (!currentEditingBox.data.Items) currentEditingBox.data.Items = [];
    currentEditingBox.data.Items.push({ ItemID: 1, Number: 1, Rate: -1, Info: "Phần thưởng mới", IsPreview: false });
    renderBoxEditorTable(currentEditingBox.data.Items);
}

function deleteBoxItemRow(idx) {
    if (!currentEditingBox || !currentEditingBox.data) return;
    gatherBoxEditorData(currentEditingBox.data);
    currentEditingBox.data.Items.splice(idx, 1);
    renderBoxEditorTable(currentEditingBox.data.Items);
}

async function saveBoxEditorChanges() {
    if (!currentEditingBox || !currentEditingBox.data) return;
    gatherBoxEditorData(currentEditingBox.data);

    const isExport = getExportState();
    let endpoint = '';
    let postData = [];

    if (currentEditingBox.type === 'DayRecharge') {
        endpoint = '/api/recharge/randomboxes-packages';
        postData = randomboxesPackages;
    } else if (currentEditingBox.type === 'AccRecharge') {
        endpoint = '/api/recharge/randomboxes-milestones';
        postData = randomboxesMilestones;
    } else if (currentEditingBox.type === 'FlowerTower') {
        endpoint = '/api/flowertower/randomboxes';
        postData = randomboxesFlowerTower;
    } else if (currentEditingBox.type === 'General') {
        endpoint = '/api/general/randomboxes';
        postData = randomboxesGeneral;
    }

    try {
        const response = await fetch(endpoint, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ isExport, data: postData })
        });
        if (response.ok) {
            showAlert(`Lưu thay đổi nội dung Box ${currentEditingBox.id} thành công!`, 'success');
            // Refresh list if visible
            if (currentEditingBox.type === 'DayRecharge') renderRandomBoxFile('randombox-packages-container', randomboxesPackages, 'DayRecharge');
            else if (currentEditingBox.type === 'AccRecharge') renderRandomBoxFile('randombox-milestones-container', randomboxesMilestones, 'AccRecharge');

            const modal = bootstrap.Modal.getInstance(document.getElementById('boxEditorModal'));
            modal.hide();
        } else {
            throw new Error(await response.text());
        }
    } catch (err) {
        alert('Lỗi khi lưu Box: ' + err.message);
    }
}

// ----------------------------------------------------
// LUCKY SPIN (VongQuayMayMan.xml)
// ----------------------------------------------------
async function loadLuckySpin() {
    try {
        const response = await fetch('/api/lucky-spin');
        if (!response.ok) throw new Error('Failed to fetch LuckySpin data');
        luckySpinData = await response.json();
        renderLuckySpin(luckySpinData);
    } catch (error) {
        console.error(error);
        const container = document.getElementById('luckyspin-form');
        if (container) container.innerHTML = `<p class="text-danger text-center">Lỗi: ${error.message}</p>`;
    }
}

function renderLuckySpin(data) {
    const container = document.getElementById('luckyspin-form');
    if (!container) return;

    const root = data.VongQuayMayMan || {};
    const attr = root.$ || {};

    let html = `
<div class="row g-3 mb-4">
    <!-- General Settings -->
    <div class="col-lg-7">
        <div class="card h-100 shadow-sm border-0">
            <div class="card-header bg-dark text-white fw-bold py-2"><i class="bi bi-gear-fill me-1"></i> 1. Thông số chung</div>
            <div class="card-body p-3">
                <div class="row g-2">
                    <div class="col-md-3">
                        <label class="form-label small fw-bold text-muted">TimeKey</label>
                        <input type="text" class="form-control form-control-sm ls-root-attr" data-attr="TimeKey" value="${attr.TimeKey || ''}">
                    </div>
                    <div class="col-md-3">
                        <label class="form-label small fw-bold text-muted">Điểm/Lượt</label>
                        <input type="number" class="form-control form-control-sm ls-root-attr" data-attr="PointPerSpin" value="${attr.PointPerSpin || '0'}">
                    </div>
                    <div class="col-md-3">
                        <label class="form-label small fw-bold text-muted">Ô trống (Slot)</label>
                        <input type="number" class="form-control form-control-sm ls-root-attr" data-attr="TotalSpaceNeed" value="${attr.TotalSpaceNeed || '0'}">
                    </div>
                    <div class="col-md-3">
                        <label class="form-label small fw-bold text-muted">Vòng quay (Min-Max)</label>
                        <div class="input-group input-group-sm">
                            <input type="number" class="form-control ls-root-attr text-center px-1" data-attr="MinSpinRounds" value="${attr.MinSpinRounds || '0'}">
                            <input type="number" class="form-control ls-root-attr text-center px-1" data-attr="MaxSpinRounds" value="${attr.MaxSpinRounds || '0'}">
                        </div>
                    </div>
                    <div class="col-md-12 mt-2">
                        <label class="form-label small fw-bold text-muted">Thời gian quay (Min-Max ms)</label>
                        <div class="input-group input-group-sm w-50">
                            <span class="input-group-text py-0"><small>MS</small></span>
                            <input type="number" class="form-control ls-root-attr text-center px-1" data-attr="MinSpinDuration" value="${attr.MinSpinDuration || '0'}">
                            <input type="number" class="form-control ls-root-attr text-center px-1" data-attr="MaxSpinDuration" value="${attr.MaxSpinDuration || '0'}">
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- Fees -->
    <div class="col-lg-5">
        <div class="card h-100 shadow-sm border-0">
            <div class="card-header bg-primary text-white fw-bold d-flex justify-content-between align-items-center py-2">
                <span><i class="bi bi-cash-coin me-1"></i> 2. Chi phí quay</span>
                <button class="btn btn-xs btn-light py-0 px-2" onclick="addLuckySpinFee()"><i class="bi bi-plus"></i></button>
            </div>
            <div class="card-body p-0">
                <table class="table table-sm table-hover mb-0" id="ls-fees-table">
                    <thead class="table-light">
                        <tr class="small text-muted">
                            <th>Loại tiền</th>
                            <th>Số lượng</th>
                            <th>Số lượt</th>
                            <th width="30"></th>
                        </tr>
                    </thead>
                    <tbody>`;
    
    if (root.Fees && root.Fees[0] && root.Fees[0].Fee) {
        root.Fees[0].Fee.forEach((fee, idx) => {
            const fAttr = fee.$ || {};
            html += `<tr>
                <td>
                    <select class="form-select form-select-sm ls-fee-attr" data-idx="${idx}" data-attr="id">
                        <option value="2" ${fAttr.id == '2' ? 'selected' : ''}>KNB (2)</option>
                        <option value="1" ${fAttr.id == '1' ? 'selected' : ''}>Bạc (1)</option>
                        <option value="0" ${fAttr.id == '0' ? 'selected' : ''}>Bạc Khóa(0)</option>
                    </select>
                </td>
                <td><input type="number" class="form-control form-control-sm ls-fee-attr" data-idx="${idx}" data-attr="amount" value="${fAttr.amount || 0}"></td>
                <td><input type="number" class="form-control form-control-sm ls-fee-attr" data-idx="${idx}" data-attr="spinTimes" value="${fAttr.spinTimes || 1}"></td>
                <td class="text-center"><button class="btn btn-link text-danger p-0" onclick="this.closest('tr').remove()"><i class="bi bi-trash"></i></button></td>
            </tr>`;
        });
    }

    html += `</tbody></table></div></div></div></div>

        <div class="card mb-4 shadow-sm border-0">
            <div class="card-header bg-success text-white fw-bold d-flex justify-content-between align-items-center py-2">
                <span><i class="bi bi-gift-fill me-1"></i> 3. Danh sách quà quay (Rates)</span>
                <button class="btn btn-xs btn-light py-0 px-2" onclick="addLuckySpinRate()"><i class="bi bi-plus"></i></button>
            </div>
            <div class="card-body p-0">
                <table class="table table-sm table-hover table-bordered mb-0" id="ls-rates-table">
                    <thead class="table-light">
                        <tr class="small text-muted">
                            <th width="60">Index</th>
                            <th>Vật phẩm (ID)</th>
                            <th width="120">Loại (Type)</th>
                            <th width="100">Số lượng</th>
                            <th width="95">Khóa</th>
                            <th width="100">Trọng số</th>
                            <th width="100">Thông báo</th>
                            <th width="40"></th>
                        </tr>
                    </thead>
                    <tbody>`;
    
    if (root.Rates && root.Rates[0] && root.Rates[0].Rate) {
        root.Rates[0].Rate.forEach((rate, idx) => {
            const rAttr = rate.$ || {};
            const item = scriptItemsGeneral.find(i => i.ItemID == rAttr.item);
            const itemName = item ? item.Name : (rAttr.type === 'money' ? 'Tiền game' : 'Unknown');

            html += `<tr>
                <td><input type="number" class="form-control form-control-sm ls-rate-attr" data-idx="${idx}" data-attr="index" value="${rAttr.index || idx+1}"></td>
                <td><input type="number" class="form-control form-control-sm ls-rate-attr item-id-input" data-idx="${idx}" data-attr="item" value="${rAttr.item || 0}"></td>
                <td>
                    <select class="form-select form-select-sm ls-rate-attr" data-idx="${idx}" data-attr="type">
                        <option value="item" ${rAttr.type === 'item' ? 'selected' : ''}>Vật phẩm</option>
                        <option value="money" ${rAttr.type === 'money' ? 'selected' : ''}>Tiền</option>
                        <option value="jackpot_big" ${rAttr.type === 'jackpot_big' ? 'selected' : ''}>Hũ Lớn</option>
                        <option value="jackpot_small" ${rAttr.type === 'jackpot_small' ? 'selected' : ''}>Hũ Nhỏ</option>
                    </select>
                </td>
                <td><input type="number" class="form-control form-control-sm ls-rate-attr" data-idx="${idx}" data-attr="amount" value="${rAttr.amount || 1}"></td>
                <td>
                    <select class="form-select form-select-sm ls-rate-attr" data-idx="${idx}" data-attr="binding">
                        <option value="1" ${rAttr.binding !== '0' ? 'selected' : ''}>Khóa</option>
                        <option value="0" ${rAttr.binding === '0' ? 'selected' : ''}>Không khóa</option>
                    </select>
                </td>
                <td><input type="number" class="form-control form-control-sm ls-rate-attr" data-idx="${idx}" data-attr="weight" value="${rAttr.weight || 100}"></td>
                <td>
                    <select class="form-select form-select-sm ls-rate-attr" data-idx="${idx}" data-attr="notify">
                        <option value="true" ${rAttr.notify === 'true' ? 'selected' : ''}>Có</option>
                        <option value="false" ${rAttr.notify === 'false' ? 'selected' : ''}>Không</option>
                    </select>
                </td>
                <td class="text-center"><button class="btn btn-link text-danger p-0" onclick="this.closest('tr').remove()"><i class="bi bi-trash"></i></button></td>
            </tr>`;
        });
    }

    html += `</tbody></table></div></div>

<div class="row g-3 mb-4">
    <div class="col-xl-6">
        <div class="card h-100 shadow-sm border-0">
            <div class="card-header bg-info text-white fw-bold d-flex justify-content-between align-items-center py-2">
                <span><i class="bi bi-bar-chart-fill me-1"></i> 4. Quà tích lũy (Accumulate)</span>
                <button class="btn btn-xs btn-light py-0 px-2" onclick="addLuckySpinMilestone()"><i class="bi bi-plus"></i></button>
            </div>
            <div class="card-body p-2" style="max-height: 400px; overflow-y: auto;">
                <div id="ls-accumulate-container">`;

    if (root.Accumulate && root.Accumulate[0] && root.Accumulate[0].Milestone) {
        root.Accumulate[0].Milestone.forEach((milestone, mIdx) => {
            const mAttr = milestone.$ || {};
            html += `
                <div class="card mb-2 border-info ls-milestone" data-m-idx="${mIdx}">
                    <div class="card-header bg-light d-flex justify-content-between align-items-center py-1 px-2 border-bottom-0">
                        <div class="d-flex gap-2 align-items-center flex-grow-1">
                            <span class="badge bg-info">Mốc ${mAttr.index || mIdx+1}</span>
                            <div class="input-group input-group-sm w-50">
                                <span class="input-group-text py-0"><small>Điểm</small></span>
                                <input type="number" class="form-control form-control-sm fw-bold m-attr py-0 text-center" data-m-idx="${mIdx}" data-attr="point" value="${mAttr.point || 0}">
                                <input type="hidden" class="m-attr" data-m-idx="${mIdx}" data-attr="index" value="${mAttr.index || mIdx+1}">
                            </div>
                        </div>
                        <button class="btn btn-sm text-danger py-0 px-1" title="Xóa mốc" onclick="this.closest('.ls-milestone').remove()"><i class="bi bi-x-lg"></i></button>
                    </div>
                    <div class="card-body p-1 pt-0">
                        <table class="table table-sm table-borderless mb-0">
                            <thead>
                                <tr class="small text-muted border-top border-bottom">
                                    <th>Vật phẩm</th>
                                    <th width="100">Loại</th>
                                    <th width="80" class="text-center">Số lượng</th>
                                    <th width="90">Khóa</th>
                                    <th width="30" class="text-center"><button class="btn btn-xs btn-outline-success p-0 px-1" title="Thêm phần thưởng" onclick="addLuckySpinMilestoneReward(${mIdx})"><i class="bi bi-plus"></i></button></th>
                                </tr>
                            </thead>
                            <tbody class="ls-milestone-rewards" data-m-idx="${mIdx}">`;
            
            if (milestone.Reward) {
                milestone.Reward.forEach((reward, rIdx) => {
                    const rAttr = reward.$ || {};
                    const item = scriptItemsGeneral.find(i => i.ItemID == rAttr.item);
                    const itemName = item ? item.Name : 'Item';
                    html += `
                        <tr>
                            <td><input type="number" class="form-control form-control-sm mr-attr item-id-input" data-m-idx="${mIdx}" data-r-idx="${rIdx}" data-attr="item" value="${rAttr.item || 0}"></td>
                            <td>
                                <select class="form-select form-select-sm mr-attr" data-m-idx="${mIdx}" data-r-idx="${rIdx}" data-attr="type">
                                    <option value="item" ${rAttr.type === 'item' ? 'selected' : ''}>Item</option>
                                    <option value="money" ${rAttr.type === 'money' ? 'selected' : ''}>Money</option>
                                </select>
                            </td>
                            <td><input type="number" class="form-control form-control-sm mr-attr" data-m-idx="${mIdx}" data-r-idx="${rIdx}" data-attr="amount" value="${rAttr.amount || 1}"></td>
                            <td>
                                <select class="form-select form-select-sm mr-attr" data-m-idx="${mIdx}" data-r-idx="${rIdx}" data-attr="binding">
                                    <option value="1" ${rAttr.binding !== '0' ? 'selected' : ''}>Khóa</option>
                                    <option value="0" ${rAttr.binding === '0' ? 'selected' : ''}>Không khóa</option>
                                </select>
                            </td>
                            <td class="text-center"><button class="btn btn-link text-danger p-0" onclick="this.closest('tr').remove()"><i class="bi bi-x-circle"></i></button></td>
                        </tr>
                    `;
                });
            }

            html += `</tbody></table></div></div>`;
        });
    }

    html += `</div></div></div></div>

    <div class="col-xl-6">
        <div class="card h-100 shadow-sm border-0">
            <div class="card-header bg-danger text-white fw-bold d-flex justify-content-between align-items-center py-2">
                <span><i class="bi bi-trophy-fill me-1"></i> 5. Thưởng xếp hạng (TopReward)</span>
                <button class="btn btn-xs btn-light py-0 px-2" onclick="addLuckySpinRanking()"><i class="bi bi-plus"></i></button>
            </div>
            <div class="card-body p-2" style="max-height: 400px; overflow-y: auto;">
                <div id="ls-ranking-container">`;

    if (root.TopReward && root.TopReward[0] && root.TopReward[0].Ranking) {
        root.TopReward[0].Ranking.forEach((ranking, ranIdx) => {
            const raAttr = ranking.$ || {};
            html += `
                <div class="card mb-2 border-danger ls-ranking" data-ran-idx="${ranIdx}">
                    <div class="card-header bg-light d-flex justify-content-between align-items-center py-1 px-2 border-bottom-0">
                        <div class="d-flex gap-2 align-items-center flex-grow-1">
                            <span class="badge bg-danger">Hạng</span>
                            <div class="input-group input-group-sm w-75">
                                <input type="number" class="form-control form-control-sm ra-attr py-0 text-center" placeholder="Từ" data-ran-idx="${ranIdx}" data-attr="from" value="${raAttr.from || 0}" style="max-width: 50px;">
                                <span class="input-group-text py-0 px-1">-</span>
                                <input type="number" class="form-control form-control-sm ra-attr py-0 text-center" placeholder="Đến" data-ran-idx="${ranIdx}" data-attr="to" value="${raAttr.to || 0}" style="max-width: 50px;">
                                <span class="input-group-text py-0 px-1"><small>Min</small></span>
                                <input type="number" class="form-control form-control-sm ra-attr py-0 text-center" data-ran-idx="${ranIdx}" data-attr="minPoint" value="${raAttr.minPoint || 0}">
                            </div>
                        </div>
                        <button class="btn btn-sm text-danger py-0 px-1" title="Xóa rank" onclick="this.closest('.ls-ranking').remove()"><i class="bi bi-x-lg"></i></button>
                    </div>
                    <div class="card-body p-1 pt-0">
                        <table class="table table-sm table-borderless mb-0">
                            <thead>
                                <tr class="small text-muted border-top border-bottom">
                                    <th>Vật phẩm</th>
                                    <th width="100">Loại</th>
                                    <th width="80" class="text-center">Số lượng</th>
                                    <th width="90">Khóa</th>
                                    <th width="30" class="text-center"><button class="btn btn-xs btn-outline-success p-0 px-1" title="Thêm phần thưởng" onclick="addLuckySpinRankingReward(${ranIdx})"><i class="bi bi-plus"></i></button></th>
                                </tr>
                            </thead>
                            <tbody class="ls-ranking-rewards" data-ran-idx="${ranIdx}">`;
            
            if (ranking.Reward) {
                ranking.Reward.forEach((reward, rIdx) => {
                    const rAttr = reward.$ || {};
                    const item = scriptItemsGeneral.find(i => i.ItemID == rAttr.item);
                    const itemName = item ? item.Name : 'Item';
                    html += `
                        <tr>
                            <td><input type="number" class="form-control form-control-sm rar-attr item-id-input" data-ran-idx="${ranIdx}" data-r-idx="${rIdx}" data-attr="item" value="${rAttr.item || 0}"></td>
                            <td>
                                <select class="form-select form-select-sm rar-attr" data-ran-idx="${ranIdx}" data-r-idx="${rIdx}" data-attr="type">
                                    <option value="item" ${rAttr.type === 'item' ? 'selected' : ''}>Item</option>
                                    <option value="money" ${rAttr.type === 'money' ? 'selected' : ''}>Money</option>
                                </select>
                            </td>
                            <td><input type="number" class="form-control form-control-sm rar-attr" data-ran-idx="${ranIdx}" data-r-idx="${rIdx}" data-attr="amount" value="${rAttr.amount || 1}"></td>
                            <td>
                                <select class="form-select form-select-sm rar-attr" data-ran-idx="${ranIdx}" data-r-idx="${rIdx}" data-attr="binding">
                                    <option value="1" ${rAttr.binding !== '0' ? 'selected' : ''}>Khóa</option>
                                    <option value="0" ${rAttr.binding === '0' ? 'selected' : ''}>Không khóa</option>
                                </select>
                            </td>
                            <td class="text-center"><button class="btn btn-link text-danger p-0" onclick="this.closest('tr').remove()"><i class="bi bi-x-circle"></i></button></td>
                        </tr>
                    `;
                });
            }

    html += `</tbody></table></div></div>`;
        });
    }

    html += `</div></div></div></div></div>

        <div class="card shadow-sm border-0 mb-4">
            <div class="card-header bg-secondary text-white fw-bold py-2"><i class="bi bi-journal-text me-1"></i> 6. Quy tắc sự kiện (Rule CDATA)</div>
            <div class="card-body p-2">
                <textarea id="ls-rule-cdata" class="form-control border-0 bg-light" rows="6" style="font-family: monospace; font-size: 0.85rem">${root.Rule ? root.Rule[0] : ''}</textarea>
            </div>
        </div>
    `;

    container.innerHTML = html;

    // Item name display removed per user request
}

// Button Handlers for LuckySpin
function addLuckySpinFee() {
    const tbody = document.querySelector('#ls-fees-table tbody');
    const tr = document.createElement('tr');
    tr.innerHTML = `
        <td>
            <select class="form-select form-select-sm ls-fee-attr" data-attr="id">
                <option value="2">KNB (2)</option>
                <option value="1">Bạc (1)</option>
                <option value="0">Bạc Khóa(0)</option>
            </select>
        </td>
        <td><input type="number" class="form-control form-control-sm ls-fee-attr" data-attr="amount" value="30"></td>
        <td><input type="number" class="form-control form-control-sm ls-fee-attr" data-attr="spinTimes" value="1"></td>
        <td class="text-center"><button class="btn btn-link text-danger p-0" onclick="this.closest('tr').remove()"><i class="bi bi-trash"></i></button></td>
    `;
    tbody.appendChild(tr);
}

function addLuckySpinRate() {
    const tbody = document.querySelector('#ls-rates-table tbody');
    const tr = document.createElement('tr');
    tr.innerHTML = `
        <td><input type="number" class="form-control form-control-sm ls-rate-attr" data-attr="index" value="${tbody.children.length + 1}"></td>
        <td><input type="number" class="form-control form-control-sm ls-rate-attr item-id-input" data-attr="item" value="1"></td>
        <td>
            <select class="form-select form-select-sm ls-rate-attr" data-attr="type">
                <option value="item">Vật phẩm</option>
                <option value="money">Tiền</option>
                <option value="jackpot_big">Hũ Lớn</option>
                <option value="jackpot_small">Hũ Nhỏ</option>
            </select>
        </td>
        <td><input type="number" class="form-control form-control-sm ls-rate-attr" data-attr="amount" value="1"></td>
        <td>
            <select class="form-select form-select-sm ls-rate-attr" data-attr="binding">
                <option value="1" selected>Khóa</option>
                <option value="0">Không khóa</option>
            </select>
        </td>
        <td><input type="number" class="form-control form-control-sm ls-rate-attr" data-attr="weight" value="100"></td>
        <td>
            <select class="form-select form-select-sm ls-rate-attr" data-attr="notify">
                <option value="true">Có</option>
                <option value="false" selected>Không</option>
            </select>
        </td>
        <td class="text-center"><button class="btn btn-link text-danger p-0" onclick="this.closest('tr').remove()"><i class="bi bi-trash"></i></button></td>
    `;
    tbody.appendChild(tr);
}

function addLuckySpinMilestone() {
    const container = document.getElementById('ls-accumulate-container');
    const mIdx = container.querySelectorAll('.ls-milestone').length;
    const div = document.createElement('div');
    div.className = 'card mb-2 border-info ls-milestone';
    div.innerHTML = `
        <div class="card-header bg-light d-flex justify-content-between align-items-center py-1 px-2 border-bottom-0">
            <div class="d-flex gap-2 align-items-center flex-grow-1">
                <span class="badge bg-info">Mốc mới</span>
                <div class="input-group input-group-sm w-50">
                    <span class="input-group-text py-0"><small>Điểm</small></span>
                    <input type="number" class="form-control form-control-sm fw-bold m-attr py-0 text-center" data-attr="point" value="1000">
                    <input type="hidden" class="m-attr" data-attr="index" value="${mIdx + 1}">
                </div>
            </div>
            <button class="btn btn-sm text-danger py-0 px-1" title="Xóa mốc" onclick="this.closest('.ls-milestone').remove()"><i class="bi bi-x-lg"></i></button>
        </div>
        <div class="card-body p-1 pt-0">
            <table class="table table-sm table-borderless mb-0">
                <thead>
                    <tr class="small text-muted border-top border-bottom">
                        <th>Vật phẩm</th>
                        <th width="100">Loại</th>
                        <th width="80" class="text-center">Số lượng</th>
                        <th width="90">Khóa</th>
                        <th width="30" class="text-center"><button class="btn btn-xs btn-outline-success p-0 px-1" title="Thêm phần thưởng" onclick="addLuckySpinMilestoneRewardDirect(this)"><i class="bi bi-plus"></i></button></th>
                    </tr>
                </thead>
                <tbody class="ls-milestone-rewards">
                    <tr>
                        <td><input type="number" class="form-control form-control-sm mr-attr item-id-input" data-attr="item" value="1"></td>
                        <td>
                            <select class="form-select form-select-sm mr-attr" data-attr="type">
                                <option value="item">Item</option>
                                <option value="money">Money</option>
                            </select>
                        </td>
                        <td><input type="number" class="form-control form-control-sm mr-attr" data-attr="amount" value="1"></td>
                        <td>
                            <select class="form-select form-select-sm mr-attr" data-attr="binding">
                                <option value="1" selected>Khóa</option>
                                <option value="0">Không khóa</option>
                            </select>
                        </td>
                        <td class="text-center"><button class="btn btn-link text-danger p-0" onclick="this.closest('tr').remove()"><i class="bi bi-x-circle"></i></button></td>
                    </tr>
                </tbody>
            </table>
        </div>
    `;
    container.appendChild(div);
}

function addLuckySpinMilestoneReward(mIdx) {
    const tbody = document.querySelector(`.ls-milestone-rewards[data-m-idx="${mIdx}"]`);
    addLuckySpinMilestoneRewardToBody(tbody);
}

function addLuckySpinMilestoneRewardDirect(btn) {
    const tbody = btn.closest('table').querySelector('tbody');
    addLuckySpinMilestoneRewardToBody(tbody);
}

function addLuckySpinMilestoneRewardToBody(tbody) {
    const tr = document.createElement('tr');
    tr.innerHTML = `
        <td><input type="number" class="form-control form-control-sm mr-attr item-id-input" data-attr="item" value="1"></td>
        <td>
            <select class="form-select form-select-sm mr-attr" data-attr="type">
                <option value="item">Item</option>
                <option value="money">Money</option>
            </select>
        </td>
        <td><input type="number" class="form-control form-control-sm mr-attr" data-attr="amount" value="1"></td>
        <td>
            <select class="form-select form-select-sm mr-attr" data-attr="binding">
                <option value="1" selected>Khóa</option>
                <option value="0">Không khóa</option>
            </select>
        </td>
        <td class="text-center"><button class="btn btn-link text-danger p-0" onclick="this.closest('tr').remove()"><i class="bi bi-x-circle"></i></button></td>
    `;
    tbody.appendChild(tr);
}

function addLuckySpinRanking() {
    const container = document.getElementById('ls-ranking-container');
    const ranIdx = container.querySelectorAll('.ls-ranking').length;
    const div = document.createElement('div');
    div.className = 'card mb-2 border-danger ls-ranking';
    div.innerHTML = `
        <div class="card-header bg-light d-flex justify-content-between align-items-center py-1 px-2 border-bottom-0">
            <div class="d-flex gap-2 align-items-center flex-grow-1">
                <span class="badge bg-danger">Hạng</span>
                <div class="input-group input-group-sm w-75">
                    <input type="number" class="form-control form-control-sm ra-attr py-0 text-center" placeholder="Từ" data-attr="from" value="99" style="max-width: 50px;">
                    <span class="input-group-text py-0 px-1">-</span>
                    <input type="number" class="form-control form-control-sm ra-attr py-0 text-center" placeholder="Đến" data-attr="to" value="100" style="max-width: 50px;">
                    <span class="input-group-text py-0 px-1"><small>Min</small></span>
                    <input type="number" class="form-control form-control-sm ra-attr py-0 text-center" data-attr="minPoint" value="1000">
                </div>
            </div>
            <button class="btn btn-sm text-danger py-0 px-1" title="Xóa rank" onclick="this.closest('.ls-ranking').remove()"><i class="bi bi-x-lg"></i></button>
        </div>
        <div class="card-body p-1 pt-0">
            <table class="table table-sm table-borderless mb-0">
                <thead>
                    <tr class="small text-muted border-top border-bottom">
                        <th>Vật phẩm</th>
                        <th width="100">Loại</th>
                        <th width="80" class="text-center">Số lượng</th>
                        <th width="90">Khóa</th>
                        <th width="30" class="text-center"><button class="btn btn-xs btn-outline-success p-0 px-1" title="Thêm phần thưởng" onclick="addLuckySpinRankingRewardDirect(this)"><i class="bi bi-plus"></i></button></th>
                    </tr>
                </thead>
                <tbody class="ls-ranking-rewards">
                    <tr>
                        <td><input type="number" class="form-control form-control-sm rar-attr item-id-input" data-attr="item" value="1"></td>
                        <td>
                            <select class="form-select form-select-sm rar-attr" data-attr="type">
                                <option value="item">Item</option>
                                <option value="money">Money</option>
                            </select>
                        </td>
                        <td><input type="number" class="form-control form-control-sm rar-attr" data-attr="amount" value="1"></td>
                        <td>
                            <select class="form-select form-select-sm rar-attr" data-attr="binding">
                                <option value="1" selected>Khóa</option>
                                <option value="0">Không khóa</option>
                            </select>
                        </td>
                        <td class="text-center"><button class="btn btn-link text-danger p-0" onclick="this.closest('tr').remove()"><i class="bi bi-x-circle"></i></button></td>
                    </tr>
                </tbody>
            </table>
        </div>
    `;
    container.appendChild(div);
}

function addLuckySpinRankingReward(ranIdx) {
    const tbody = document.querySelector(`.ls-ranking-rewards[data-ran-idx="${ranIdx}"]`);
    addLuckySpinRankingRewardToBody(tbody);
}

function addLuckySpinRankingRewardDirect(btn) {
    const tbody = btn.closest('table').querySelector('tbody');
    addLuckySpinRankingRewardToBody(tbody);
}

function addLuckySpinRankingRewardToBody(tbody) {
    const tr = document.createElement('tr');
    tr.innerHTML = `
        <td><input type="number" class="form-control form-control-sm rar-attr item-id-input" data-attr="item" value="1"></td>
        <td>
            <select class="form-select form-select-sm rar-attr" data-attr="type">
                <option value="item">Item</option>
                <option value="money">Money</option>
            </select>
        </td>
        <td><input type="number" class="form-control form-control-sm rar-attr" data-attr="amount" value="1"></td>
        <td>
            <select class="form-select form-select-sm rar-attr" data-attr="binding">
                <option value="1" selected>Khóa</option>
                <option value="0">Không khóa</option>
            </select>
        </td>
        <td class="text-center"><button class="btn btn-link text-danger p-0" onclick="this.closest('tr').remove()"><i class="bi bi-x-circle"></i></button></td>
    `;
    tbody.appendChild(tr);
}

async function saveLuckySpin() {
    if (!luckySpinData) return;

    // 1. Root attributes
    const root = luckySpinData.VongQuayMayMan;
    if (!root.$) root.$ = {};
    document.querySelectorAll('.ls-root-attr').forEach(input => {
        root.$[input.dataset.attr] = input.value;
    });

    // 2. Fees
    root.Fees = [{ Fee: [] }];
    document.querySelectorAll('#ls-fees-table tbody tr').forEach(tr => {
        const fee = { $: {} };
        tr.querySelectorAll('.ls-fee-attr').forEach(input => {
            fee.$[input.dataset.attr] = input.value;
        });
        root.Fees[0].Fee.push(fee);
    });

    // 3. Rule
    root.Rule = [document.getElementById('ls-rule-cdata').value];

    // 4. Rates
    root.Rates = [{ Rate: [] }];
    document.querySelectorAll('#ls-rates-table tbody tr').forEach(tr => {
        const rate = { $: {} };
        tr.querySelectorAll('.ls-rate-attr').forEach(input => {
            rate.$[input.dataset.attr] = input.value;
        });
        root.Rates[0].Rate.push(rate);
    });

    // 5. Accumulate
    root.Accumulate = [{ Milestone: [] }];
    document.querySelectorAll('.ls-milestone').forEach(card => {
        const milestone = { $: {}, Reward: [] };
        card.querySelectorAll('.m-attr').forEach(input => {
            milestone.$[input.dataset.attr] = input.value;
        });
        card.querySelectorAll('.ls-milestone-rewards tr').forEach(tr => {
            const reward = { $: {} };
            tr.querySelectorAll('.mr-attr').forEach(input => {
                reward.$[input.dataset.attr] = input.value;
            });
            milestone.Reward.push(reward);
        });
        root.Accumulate[0].Milestone.push(milestone);
    });

    // 6. TopReward
    root.TopReward = [{ Ranking: [] }];
    document.querySelectorAll('.ls-ranking').forEach(card => {
        const ranking = { $: {}, Reward: [] };
        card.querySelectorAll('.ra-attr').forEach(input => {
            ranking.$[input.dataset.attr] = input.value;
        });
        card.querySelectorAll('.ls-ranking-rewards tr').forEach(tr => {
            const reward = { $: {} };
            tr.querySelectorAll('.rar-attr').forEach(input => {
                reward.$[input.dataset.attr] = input.value;
            });
            ranking.Reward.push(reward);
        });
        root.TopReward[0].Ranking.push(ranking);
    });

    luckySpinData.isExport = getExportState();

    try {
        const response = await fetch('/api/lucky-spin', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(luckySpinData)
        });
        if (response.ok) {
            showAlert('Lưu cấu hình Tân Niên Bí Bảo thành công!', 'success');
        } else throw new Error(await response.text());
    } catch (err) {
        showAlert('Lỗi: ' + err.message, 'danger');
    }
}

// ============================================
// SERVER MERGE REWARDS (ServerMergeRewards.xml)
// ============================================

async function loadServerMerge() {
    try {
        const res = await fetch('/api/server-merge');
        const data = await res.json();
        serverMergeData = data;
        renderServerMerge(data);
    } catch (err) {
        document.getElementById('servermerge-form').innerHTML = 
            `<div class="alert alert-danger">Lỗi tải cấu hình Liên Thông Server: ${err.message}</div>`;
    }
}

function renderServerMerge(data) {
    if (!data || !data.MergeRewards) return;
    
    const root = data.MergeRewards;
    const mergeRootConfig = root.$ || {};
    const packages = root.Package || [];
    
    let html = `
        <div class="row g-3 mb-4">
            <div class="col-md-3">
                <label class="form-label mb-1 fw-bold">RechargeStartDate (Bắt đầu nạp)</label>
                <input type="text" class="form-control form-control-sm sm-attr" data-attr="RechargeStartDate" value="${mergeRootConfig.RechargeStartDate || ''}">
            </div>
            <div class="col-md-3">
                <label class="form-label mb-1 fw-bold">RechargeEndDate (Kết thúc nạp)</label>
                <input type="text" class="form-control form-control-sm sm-attr" data-attr="RechargeEndDate" value="${mergeRootConfig.RechargeEndDate || ''}">
            </div>
            <div class="col-md-2">
                <label class="form-label mb-1 fw-bold">Nạp Tối Thiểu (KNB)</label>
                <input type="number" class="form-control form-control-sm sm-attr" data-attr="RechargeRequirement" value="${mergeRootConfig.RechargeRequirement || '0'}">
            </div>
            <div class="col-md-2">
                <label class="form-label mb-1 fw-bold">Cấp Phi Phong Cần</label>
                <input type="number" class="form-control form-control-sm sm-attr" data-attr="MantleRequirement" value="${mergeRootConfig.MantleRequirement || '0'}">
            </div>
            <div class="col-md-2">
                <label class="form-label mb-1 fw-bold">Set Cấp Nhận Quà</label>
                <input type="number" class="form-control form-control-sm sm-attr" data-attr="SetLevel" value="${mergeRootConfig.SetLevel || '120'}">
            </div>
        </div>
        
        <div class="d-flex justify-content-between align-items-center mt-4 mb-2">
            <h5 class="m-0 text-primary"><i class="bi bi-box2-heart"></i> Danh sách Gói Quà Liên Thông</h5>
            <button class="btn btn-sm btn-primary shadow-sm" onclick="addServerMergePackage()"><i class="bi bi-plus-circle"></i> Thêm Gói Mới</button>
        </div>
        <div id="sm-packages-container" class="row g-3">
    `;

    packages.forEach((pkg) => {
        html += generateServerMergePackageHTML(pkg);
    });

    html += `</div>`;

    document.getElementById('servermerge-form').innerHTML = html;
}

function generateServerMergePackageHTML(pkg) {
    const pAttr = pkg.$ || {};
    const items = (pkg.Items && pkg.Items[0] && pkg.Items[0].Item) ? pkg.Items[0].Item : [];
    
    let itemsHtml = '';
    items.forEach(item => {
        const itemAttr = item.$ || {};
        itemsHtml += `
            <tr>
                <td class="p-1"><input type="number" class="form-control form-control-sm smp-item-attr" data-attr="ItemID" value="${itemAttr.ItemID || ''}" onchange="updateItemName(this)"></td>
                <td class="p-1"><input type="number" class="form-control form-control-sm smp-item-attr" data-attr="Number" value="${itemAttr.Number || '1'}"></td>
                <td class="p-1">
                    <select class="form-select form-select-sm smp-item-attr" data-attr="Binding">
                        <option value="1" ${itemAttr.Binding == '1' ? 'selected' : ''}>Khóa</option>
                        <option value="0" ${itemAttr.Binding == '0' ? 'selected' : ''}>Không Khóa</option>
                    </select>
                </td>
                <td class="p-1 text-center"><button class="btn btn-sm btn-outline-danger py-0 px-2" onclick="this.closest('tr').remove()"><i class="bi bi-x-lg"></i></button></td>
            </tr>
        `;
    });

    return `
        <div class="col-12 sm-package">
            <div class="card h-100 border-primary shadow-sm">
                <div class="card-header bg-primary text-white py-1 d-flex justify-content-between align-items-center">
                    <div class="d-flex align-items-center gap-2">
                        <span class="fw-bold"><i class="bi bi-gift-fill me-1"></i> ID Gói:</span>
                        <input type="number" class="form-control form-control-sm d-inline-block smp-attr text-center" data-attr="ID" value="${pAttr.ID || ''}" style="width: 70px;">
                    </div>
                    <button class="btn btn-sm btn-danger py-0 px-2" onclick="this.closest('.sm-package').remove()" title="Xóa Gói Này"><i class="bi bi-trash3-fill"></i></button>
                </div>
                <div class="card-body p-2 pt-3">
                    <div class="row g-2 mb-3">
                        <div class="col-md-5">
                            <label class="form-label mb-0 text-muted" style="font-size: 0.8rem;">Tên Gói</label>
                            <input type="text" class="form-control form-control-sm smp-attr fw-bold" data-attr="Name" value="${pAttr.Name || ''}">
                        </div>
                        <div class="col-md-7">
                            <label class="form-label mb-0 text-muted" style="font-size: 0.8rem;" title="ID Khu vực liên thông (VD: 25,26,27)">CreatedZoneID (Các Server gom lại)</label>
                            <input type="text" class="form-control form-control-sm smp-attr" data-attr="CreatedZoneID" value="${pAttr.CreatedZoneID || ''}" placeholder="25,26,27...">
                        </div>
                        <div class="col-md-4">
                            <label class="form-label mb-0 text-muted" style="font-size: 0.8rem;">Tiền Yêu Cầu</label>
                            <input type="number" class="form-control form-control-sm smp-attr" data-attr="Money" value="${pAttr.Money || '0'}">
                        </div>
                        <div class="col-md-4">
                            <label class="form-label mb-0 text-muted" style="font-size: 0.8rem;">Loại Tiền</label>
                            <select class="form-select form-select-sm smp-attr" data-attr="MoneyType">
                                <option value="2" ${pAttr.MoneyType == '2' ? 'selected' : ''}>KNB</option>
                                <option value="1" ${pAttr.MoneyType == '1' ? 'selected' : ''}>Bạc</option>
                                <option value="0" ${pAttr.MoneyType == '0' ? 'selected' : ''}>Bạc Khóa</option>
                            </select>
                        </div>
                        <div class="col-md-2">
                            <label class="form-label mb-0 text-muted" style="font-size: 0.8rem;">Giới hạn</label>
                            <input type="number" class="form-control form-control-sm smp-attr" data-attr="Limit" value="${pAttr.Limit || '1'}">
                        </div>
                        <div class="col-md-2 d-flex align-items-end justify-content-center pb-1">
                            <div class="form-check form-switch mb-0" title="Áp dụng điều kiện Nạp/Phi Phong ở trên">
                                <input class="form-check-input smp-attr-bool" type="checkbox" data-attr="ApplyCondition" ${pAttr.ApplyCondition === 'true' ? 'checked' : ''}>
                                <label class="form-check-label" style="font-size: 0.75rem;">Kiểm đk</label>
                            </div>
                        </div>
                    </div>
                    
                    <div class="border rounded p-1">
                        <div class="d-flex justify-content-between align-items-center mb-1 bg-light px-2 py-1 rounded">
                            <span class="fw-bold text-secondary" style="font-size: 0.85rem;"><i class="bi bi-box-seam me-1"></i> Vật Phẩm</span>
                            <button class="btn btn-sm btn-outline-primary py-0 px-2" onclick="addServerMergeItem(this)"><i class="bi bi-plus-lg"></i></button>
                        </div>
                        <div style="max-height: 350px; overflow-y: auto;">
                            <table class="table table-sm table-borderless table-striped text-center mb-0 align-middle">
                                <thead style="font-size: 0.8rem;" class="text-secondary sticky-top bg-white">
                                    <tr>
                                        <th>ItemID</th>
                                        <th width="70px">SL</th>
                                        <th width="90px">Khóa</th>
                                        <th width="35px"></th>
                                    </tr>
                                </thead>
                                <tbody class="smp-items-list border-top">
                                    ${itemsHtml}
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `;
}

function addServerMergePackage() {
    const container = document.getElementById('sm-packages-container');
    const newPkg = { $: { ID: '', CreatedZoneID: '', Name: 'Gói Liên Thông Mới', MoneyType: '2', Money: '0', Limit: '1', ApplyCondition: 'false' }, Items: [{ Item: [] }] };
    container.insertAdjacentHTML('afterbegin', generateServerMergePackageHTML(newPkg));
}

function addServerMergeItem(btn) {
    const tbody = btn.closest('.card-body').querySelector('.smp-items-list');
    tbody.insertAdjacentHTML('beforeend', `
        <tr>
            <td class="p-1"><input type="number" class="form-control form-control-sm smp-item-attr" data-attr="ItemID" value="" onchange="updateItemName(this)"></td>
            <td class="p-1"><input type="number" class="form-control form-control-sm smp-item-attr" data-attr="Number" value="1"></td>
            <td class="p-1">
                <select class="form-select form-select-sm smp-item-attr" data-attr="Binding">
                    <option value="1" selected>Khóa</option>
                    <option value="0">Không Khóa</option>
                </select>
            </td>
            <td class="p-1 text-center"><button class="btn btn-sm btn-outline-danger py-0 px-2" onclick="this.closest('tr').remove()"><i class="bi bi-x-lg"></i></button></td>
        </tr>
    `);
}

async function saveServerMerge() {
    if (!serverMergeData || !serverMergeData.MergeRewards) {
        showAlert('Chưa có dữ liệu Server Merge! Hãy tải lại trang.', 'danger');
        return;
    }

    const root = serverMergeData.MergeRewards;
    root.$ = root.$ || {};

    // 1. Root Attrs
    document.querySelectorAll('.sm-attr').forEach(input => {
        root.$[input.dataset.attr] = input.value;
    });

    // 2. Packages
    root.Package = [];
    document.querySelectorAll('.sm-package').forEach(card => {
        const pkg = { $: {}, Items: [{ Item: [] }] };
        card.querySelectorAll('.smp-attr').forEach(input => {
            pkg.$[input.dataset.attr] = input.value;
        });
        card.querySelectorAll('.smp-attr-bool').forEach(input => {
            pkg.$[input.dataset.attr] = input.checked ? 'true' : 'false';
        });

        card.querySelectorAll('.smp-items-list tr').forEach(tr => {
            const item = { $: {} };
            tr.querySelectorAll('.smp-item-attr').forEach(input => {
                item.$[input.dataset.attr] = input.value;
            });
            pkg.Items[0].Item.push(item);
        });

        root.Package.push(pkg);
    });

    serverMergeData.isExport = getExportState();

    try {
        const response = await fetch('/api/server-merge', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(serverMergeData)
        });
        if (response.ok) {
            showAlert('Lưu cấu hình Liên Thông Server thành công!', 'success');
        } else throw new Error(await response.text());
    } catch (err) {
        showAlert('Lỗi: ' + err.message, 'danger');
    }
}

/* ========================================================================================= */
/* BATTLE PASS SETTINGS                                                                      */
/* ========================================================================================= */
async function loadBattlePass() {
    try {
        const response = await fetch('/api/battle-pass');
        battlePassData = await response.json();
        renderBattlePass();
    } catch (error) {
        console.error('Error loading Battle Pass config:', error);
        document.getElementById('battlepass-form').innerHTML = `<div class="alert alert-danger">Lỗi tải cấu hình Battle Pass: ${error.message}</div>`;
    }
}

function renderBattlePass() {
    if (!battlePassData || !battlePassData.BattlePass) return;
    const root = battlePassData.BattlePass;
    const bAttr = root.$ || {};
    
    // Ensure arrays exist
    if (!root.Levels) root.Levels = [{ Level: [] }];
    if (!root.Tasks) root.Tasks = [{ Task: [] }];
    if (!root.Bundles) root.Bundles = [{ Bundle: [] }];
    if (!root.BonusChest) root.BonusChest = [{ $: { ExpRequired: '1000' } }];

    // 1. General Info & Bonus Chest
    const bonusChest = root.BonusChest[0];
    const bcAttr = bonusChest.$ || {};
    let bcRewardsHtml = '';
    
    let bonusChestRewards = bonusChest.Reward || [];
    bonusChestRewards.forEach(r => {
        bcRewardsHtml += generateBpRewardRowHtml(r, 'Reward');
    });
    
    let bonusChestElites = bonusChest.RewardElite || [];
    bonusChestElites.forEach(r => {
        bcRewardsHtml += generateBpRewardRowHtml(r, 'RewardElite');
    });

    let html = `
        <ul class="nav nav-pills nav-fill mb-3 bg-light rounded" id="bp-pills-tab" role="tablist" style="padding:4px;">
            <li class="nav-item" role="presentation">
                <button class="nav-link active fw-bold" id="bp-general-tab" data-bs-toggle="pill" data-bs-target="#bp-general" type="button" role="tab">Chung & Rương Thưởng</button>
            </li>
            <li class="nav-item" role="presentation">
                <button class="nav-link fw-bold" id="bp-levels-tab" data-bs-toggle="pill" data-bs-target="#bp-levels" type="button" role="tab">Cấp Độ (Levels)</button>
            </li>
            <li class="nav-item" role="presentation">
                <button class="nav-link fw-bold" id="bp-tasks-tab" data-bs-toggle="pill" data-bs-target="#bp-tasks" type="button" role="tab">Nhiệm Vụ (Tasks)</button>
            </li>
            <li class="nav-item" role="presentation">
                <button class="nav-link fw-bold" id="bp-bundles-tab" data-bs-toggle="pill" data-bs-target="#bp-bundles" type="button" role="tab">Gói Mua (Bundles)</button>
            </li>
        </ul>

        <div class="tab-content" id="bp-pills-tabContent">
            <!-- Chung & Rương -->
            <div class="tab-pane fade show active" id="bp-general" role="tabpanel">
                <div class="row g-3">
                    <div class="col-md-6">
                        <div class="card border-info shadow-sm h-100">
                            <div class="card-header bg-info text-white py-2"><i class="bi bi-gear-wide-connected"></i> <strong>Cài đặt Chung</strong></div>
                            <div class="card-body">
                                <div class="mb-3">
                                    <label class="form-label mb-1 text-muted small fw-bold">Mùa Giải (SeasonID)</label>
                                    <input type="number" class="form-control bp-attr" data-attr="SeasonID" value="${bAttr.SeasonID || ''}">
                                </div>
                                <div class="row g-2">
                                    <div class="col-6">
                                        <label class="form-label mb-1 text-muted small fw-bold">Giá mở thẻ KNB (BuyLevelCost)</label>
                                        <input type="number" class="form-control bp-attr" data-attr="BuyLevelCost" value="${bAttr.BuyLevelCost || '200'}">
                                    </div>
                                    <div class="col-6">
                                        <label class="form-label mb-1 text-muted small fw-bold">EXP 1 Lần mua (BuyLevelExp)</label>
                                        <input type="number" class="form-control bp-attr" data-attr="BuyLevelExp" value="${bAttr.BuyLevelExp || '1000'}">
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="card border-warning shadow-sm h-100">
                            <div class="card-header bg-warning text-dark py-2 d-flex justify-content-between align-items-center">
                                <strong><i class="bi bi-box-fill"></i> Rương Vượt Cấp (BonusChest)</strong>
                                <button class="btn btn-sm btn-dark py-0 px-2" onclick="addBpBonusChestReward(this)"><i class="bi bi-plus-lg"></i> Thêm Quà</button>
                            </div>
                            <div class="card-body p-2">
                                <div class="mb-2 px-2">
                                    <label class="form-label mb-1 text-muted small fw-bold">EXP Lặp Lại Nhận Rương (ExpRequired)</label>
                                    <input type="number" class="form-control bp-bc-attr" data-attr="ExpRequired" value="${bcAttr.ExpRequired || '1000'}">
                                </div>
                                <hr class="my-2">
                                <table class="table table-sm table-borderless table-striped text-center mb-0 align-middle">
                                    <thead class="small text-secondary"><tr><th>Loại</th><th>ItemID</th><th>Số Lượng</th><th></th></tr></thead>
                                    <tbody id="bp-bonuschest-list">${bcRewardsHtml}</tbody>
                                </table>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Cấp Độ -->
            <div class="tab-pane fade" id="bp-levels" role="tabpanel">
                <div class="d-flex justify-content-between mb-3 align-items-center px-2">
                    <button class="btn btn-success" onclick="addBpLevel()"><i class="bi bi-plus-lg fw-bold"></i> Thêm Cấp Độ Mới (Level)</button>
                    <span class="badge bg-secondary fs-6">Tổng Cấp: ${root.Levels[0] && root.Levels[0].Level ? root.Levels[0].Level.length : 0}</span>
                </div>
                <div id="bp-levels-container" class="row g-3">
    `;

    // Render Levels
    const levels = (root.Levels[0] && root.Levels[0].Level) ? root.Levels[0].Level : [];
    levels.forEach(lvl => {
        html += generateBpLevelHtml(lvl);
    });

    html += `
                </div>
            </div>

            <!-- Nhiệm vụ -->
            <div class="tab-pane fade" id="bp-tasks" role="tabpanel">
                <div class="mb-3 px-2">
                    <button class="btn btn-success" onclick="addBpTask()"><i class="bi bi-plus-lg fw-bold"></i> Thêm Nhiệm Vụ Mới</button>
                </div>
                <div id="bp-tasks-container" class="row g-3">
    `;

    // Render Tasks
    const tasks = (root.Tasks[0] && root.Tasks[0].Task) ? root.Tasks[0].Task : [];
    tasks.forEach(task => {
        html += generateBpTaskHtml(task);
    });

    html += `
                </div>
            </div>

            <!-- Gói Bundles -->
            <div class="tab-pane fade" id="bp-bundles" role="tabpanel">
                <div class="mb-3 px-2">
                    <button class="btn btn-success" onclick="addBpBundle()"><i class="bi bi-plus-lg fw-bold"></i> Thêm Gói Mua (Bundle)</button>
                </div>
                <div id="bp-bundles-container" class="row g-4">
    `;

    // Render Bundles
    const bundles = (root.Bundles[0] && root.Bundles[0].Bundle) ? root.Bundles[0].Bundle : [];
    bundles.forEach(b => {
        html += generateBpBundleHtml(b);
    });

    html += `
                </div>
            </div>
        </div>
    `;

    document.getElementById('battlepass-form').innerHTML = html;
}

function generateBpRewardRowHtml(r, typeTag) {
    const attr = r.$ || {};
    return `
        <tr class="bp-reward-row" data-tag="${typeTag}">
            <td class="p-1" style="width:140px;">
                <select class="form-select form-select-sm bp-reward-tag" onchange="this.closest('tr').dataset.tag = this.value">
                    <option value="FreeReward" ${typeTag=='FreeReward'?'selected':''}>Thường</option>
                    <option value="EliteReward" ${typeTag=='EliteReward'?'selected':''}>Cao Cấp</option>
                    <option value="Reward" ${typeTag=='Reward'?'selected':''}>Rương Thường</option>
                    <option value="RewardElite" ${typeTag=='RewardElite'?'selected':''}>Rương Cao Cấp</option>
                    <option value="InstantReward" ${typeTag=='InstantReward'?'selected':''}>Nhận Ngay</option>
                    <option value="ExclusiveReward" ${typeTag=='ExclusiveReward'?'selected':''}>Độc Quyền Cao Cấp</option>
                    <option value="BundleExclusiveReward" ${typeTag=='BundleExclusiveReward'?'selected':''}>Độc Quyền Siêu Cấp</option>
                </select>
            </td>
            <td class="p-1"><input type="number" class="form-control form-control-sm bp-r-attr" data-attr="ItemID" value="${attr.ItemID || ''}" onchange="updateItemName(this)"></td>
            <td class="p-1" style="width:90px;"><input type="number" class="form-control form-control-sm bp-r-attr" data-attr="Number" value="${attr.Number || '1'}"></td>
            <td class="p-1 text-center" style="width:40px;"><button class="btn btn-sm btn-outline-danger py-0 px-2" onclick="this.closest('tr').remove()"><i class="bi bi-x-lg"></i></button></td>
        </tr>
    `;
}

function generateBpLevelHtml(lvl) {
    const attr = lvl.$ || {};
    let rewardsHtml = '';
    (lvl.FreeReward || []).forEach(r => rewardsHtml += generateBpRewardRowHtml(r, 'FreeReward'));
    (lvl.EliteReward || []).forEach(r => rewardsHtml += generateBpRewardRowHtml(r, 'EliteReward'));

    return `
        <div class="col-12 col-xl-6 bp-level-card">
            <div class="card shadow border-secondary">
                <div class="card-header py-2 d-flex justify-content-between align-items-center bg-light">
                    <div class="d-flex align-items-center gap-2">
                        <span class="fw-bold text-dark"><i class="bi bi-bar-chart-fill text-muted"></i> Cấp</span>
                        <input type="number" class="form-control form-control-sm text-center fw-bold bp-l-attr text-primary" data-attr="ID" value="${attr.ID || ''}" style="width:70px;">
                        <span class="ms-3 text-muted small fw-bold">EXP Tới Hạn:</span>
                        <input type="number" class="form-control form-control-sm bp-l-attr" data-attr="ExpRequired" value="${attr.ExpRequired || '1000'}" style="width:100px;">
                    </div>
                    <div>
                        <button class="btn btn-sm btn-outline-success py-1 px-2 fw-bold" onclick="addBpLevelReward(this, 'FreeReward')" title="Thêm Thường">+ Thường</button>
                        <button class="btn btn-sm btn-outline-warning py-1 px-2 fw-bold" onclick="addBpLevelReward(this, 'EliteReward')" title="Thêm Cao Cấp">+ Cao Cấp</button>
                        <button class="btn btn-sm btn-danger py-1 px-2 ms-2" title="Xóa Cấp Này" onclick="this.closest('.bp-level-card').remove()"><i class="bi bi-trash"></i></button>
                    </div>
                </div>
                <div class="card-body p-2" style="max-height: 300px; overflow-y: auto;">
                    <table class="table table-sm table-borderless table-striped text-center mb-0 align-middle">
                        <thead class="text-secondary small"><tr><th>Loại</th><th>ItemID</th><th>Số</th><th></th></tr></thead>
                        <tbody class="bp-level-rewards border-top">${rewardsHtml}</tbody>
                    </table>
                </div>
            </div>
        </div>
    `;
}

function generateBpTaskHtml(task) {
    const attr = task.$ || {};
    return `
        <div class="col-12 col-md-6 col-xl-4 bp-task-card">
            <div class="card shadow-sm border-0 border-start border-4 border-info h-100">
                <div class="card-header py-1 d-flex justify-content-between align-items-center bg-white border-bottom-0 pt-3">
                    <div class="d-flex align-items-center gap-2">
                        <span class="fw-bold text-muted small">Task ID</span>
                        <input type="number" class="form-control form-control-sm d-inline-block bp-t-attr fw-bold" data-attr="ID" value="${attr.ID || ''}" style="width:80px;">
                    </div>
                    <button class="btn btn-sm btn-outline-danger py-0 px-2" onclick="this.closest('.bp-task-card').remove()"><i class="bi bi-x-lg"></i></button>
                </div>
                <div class="card-body p-3 pt-1">
                    <label class="small text-muted mb-0">Tên Nhiệm Vụ</label>
                    <input type="text" class="form-control form-control-sm fw-bold mb-3 bp-t-attr text-primary" data-attr="Name" value="${attr.Name || ''}" placeholder="Tên Nhiệm Vụ">
                    <div class="row g-2 small">
                        <div class="col-6"><label class="text-muted d-block text-truncate">Lặp Lại (IndexRepeat)</label>
                            <select class="form-select form-select-sm bp-t-attr" data-attr="IndexRepeat">
                                <option value="0" ${attr.IndexRepeat=='0'?'selected':''}>Hằng Ngày</option>
                                <option value="1" ${attr.IndexRepeat=='1'?'selected':''}>Hằng Tuần</option>
                                <option value="2" ${attr.IndexRepeat=='2'?'selected':''}>Cả Mùa Giải</option>
                            </select>
                        </div>
                        <div class="col-6"><label class="text-muted d-block text-truncate">Loại (Type)</label>
                            <select class="form-select form-select-sm bp-t-attr" data-attr="Type">
                                <option value="0" ${attr.Type=='0'?'selected':''}>Làm 1 lần</option>
                                <option value="1" ${attr.Type=='1'?'selected':''}>Làm N lần (Gom góp)</option>
                            </select>
                        </div>
                        <div class="col-6"><label class="text-muted d-block text-truncate">Số Lần Lặp Nhỏ (NumberItem)</label>
                            <input type="number" class="form-control form-control-sm bp-t-attr" data-attr="NumberItem" value="${attr.NumberItem || '1'}">
                        </div>
                        <div class="col-6"><label class="text-muted d-block text-truncate">Kinh Nghiệm Được (Exp)</label>
                            <input type="number" class="form-control form-control-sm bp-t-attr fw-bold" style="color: green" data-attr="Exp" value="${attr.Exp || '50'}">
                        </div>
                        <div class="col-6"><label class="text-muted d-block text-truncate">Sự kiện (EventType)</label>
                            <input type="number" class="form-control form-control-sm bp-t-attr text-warning fw-bold" data-attr="EventType" value="${attr.EventType || ''}">
                        </div>
                        <div class="col-6"><label class="text-muted d-block text-truncate">Tham số (Param)</label>
                            <input type="text" class="form-control form-control-sm bp-t-attr" data-attr="Param" value="${attr.Param || '-1'}">
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `;
}

function generateBpBundleHtml(b) {
    const attr = b.$ || {};
    let rewardsHtml = '';
    (b.InstantReward || []).forEach(r => rewardsHtml += generateBpRewardRowHtml(r, 'InstantReward'));
    (b.ExclusiveReward || []).forEach(r => rewardsHtml += generateBpRewardRowHtml(r, 'ExclusiveReward'));
    (b.BundleExclusiveReward || []).forEach(r => rewardsHtml += generateBpRewardRowHtml(r, 'BundleExclusiveReward'));

    return `
        <div class="col-12 bp-bundle-card">
            <div class="card shadow-sm border-dark">
                <div class="card-header py-2 bg-dark text-white d-flex justify-content-between align-items-center">
                    <div class="d-flex align-items-center gap-2">
                        <span class="fw-bold"><i class="bi bi-cart4 text-warning"></i> ID Gói (Nạp thẻ):</span>
                        <input type="number" class="form-control form-control-sm d-inline-block text-center fw-bold bp-b-attr text-dark" data-attr="ID" value="${attr.ID || ''}" style="width:70px;">
                        <input type="text" class="form-control form-control-sm fw-bold bp-b-attr ms-3" data-attr="Title" value="${attr.Title || ''}" placeholder="Tên Gói Mua" style="width: 300px;">
                    </div>
                    <button class="btn btn-sm btn-danger py-1 px-3" onclick="this.closest('.bp-bundle-card').remove()" title="Xóa gói"><i class="bi bi-trash"></i> Xóa Gói Này</button>
                </div>
                <div class="card-body p-3">
                    <div class="row g-3 mb-3">
                        <div class="col-md-3">
                            <label class="form-label text-muted small fw-bold mb-1">Giá Nạp Thật (Price)</label>
                            <div class="input-group input-group-sm">
                                <input type="text" class="form-control bp-b-attr fw-bold" data-attr="Price" value="${attr.Price || '0'}">
                                <span class="input-group-text"><input type="text" class="form-control-plaintext form-control-sm bp-b-attr p-0" data-attr="Currency" value="${attr.Currency || 'USD'}" style="width: 40px; text-align: center;"></span>
                            </div>
                        </div>
                        <div class="col-md-3">
                            <label class="form-label text-muted small fw-bold mb-1">Tặng KNB Mốc Nạp (MoneyBonus)</label>
                            <input type="number" class="form-control form-control-sm bp-b-attr text-success fw-bold" data-attr="MoneyBonus" value="${attr.MoneyBonus || '0'}">
                        </div>
                        <div class="col-md-3">
                            <label class="form-label text-muted small fw-bold mb-1">Cấp Mở Khóa Tức Thì (LevelGain)</label>
                            <input type="number" class="form-control form-control-sm bp-b-attr text-primary fw-bold" data-attr="LevelGain" value="${attr.LevelGain || '0'}">
                        </div>
                    </div>
                    <div class="border rounded p-2 bg-light">
                        <div class="d-flex justify-content-between align-items-center mb-2 px-1">
                            <span class="fw-bold small text-dark"><i class="bi bi-gift-fill text-danger me-1"></i> Vật Phẩm Thưởng Của Gói</span>
                            <button class="btn btn-sm btn-primary py-1 px-3 fw-bold shadow-sm" onclick="addBpBundleReward(this)"><i class="bi bi-plus-lg"></i> Thêm Vật Phẩm</button>
                        </div>
                        <div style="max-height: 250px; overflow-y: auto;">
                            <table class="table table-sm table-borderless table-striped text-center mb-0 align-middle bg-white">
                                <thead class="small text-secondary shadow-sm"><tr><th>Loại Nhận (Tag)</th><th>Mã Vật Phẩm (ItemID)</th><th>Số Lượng (Number)</th><th></th></tr></thead>
                                <tbody class="bp-bundle-rewards border-top">${rewardsHtml}</tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `;
}

// Add Functions
function addBpLevel() {
    const defaultLvl = { $: { ID: '', ExpRequired: '1000'} };
    document.getElementById('bp-levels-container').insertAdjacentHTML('beforeend', generateBpLevelHtml(defaultLvl));
}
function addBpTask() {
    const defaultTask = { $: { ID: '', Name: 'Nhiệm vụ mới', Type: '0', NumberItem: '1', IndexRepeat: '0', EventType: '', Param: '-1', Exp: '50'} };
    document.getElementById('bp-tasks-container').insertAdjacentHTML('afterbegin', generateBpTaskHtml(defaultTask));
}
function addBpBundle() {
    const defaultBundle = { $: { ID: '', Title: 'Gói Mới', MoneyBonus: '0', LevelGain: '0', Price: '9.99', Currency: 'USD'} };
    document.getElementById('bp-bundles-container').insertAdjacentHTML('beforeend', generateBpBundleHtml(defaultBundle));
}
function addBpLevelReward(btn, type) {
    btn.closest('.card').querySelector('.bp-level-rewards').insertAdjacentHTML('beforeend', generateBpRewardRowHtml({$: {}}, type));
}
function addBpBundleReward(btn) {
    btn.closest('.card-body').querySelector('.bp-bundle-rewards').insertAdjacentHTML('beforeend', generateBpRewardRowHtml({$: {}}, 'ExclusiveReward'));
}
function addBpBonusChestReward(btn) {
    document.getElementById('bp-bonuschest-list').insertAdjacentHTML('beforeend', generateBpRewardRowHtml({$: {}}, 'Reward'));
}

async function saveBattlePass() {
    if (!battlePassData || !battlePassData.BattlePass) {
        showAlert('Chưa có dữ liệu Battle Pass!', 'danger');
        return;
    }

    const root = battlePassData.BattlePass;
    root.$ = root.$ || {};

    // 1. General Info
    document.querySelectorAll('.bp-attr').forEach(el => root.$[el.dataset.attr] = el.value);

    // Bonus Chest
    const bonusChest = { $: {} };
    document.querySelectorAll('.bp-bc-attr').forEach(el => bonusChest.$[el.dataset.attr] = el.value);
    document.querySelectorAll('#bp-bonuschest-list .bp-reward-row').forEach(tr => {
        const tag = tr.dataset.tag;
        if (!bonusChest[tag]) bonusChest[tag] = [];
        const r = { $: {} };
        tr.querySelectorAll('.bp-r-attr').forEach(el => r.$[el.dataset.attr] = el.value);
        bonusChest[tag].push(r);
    });
    root.BonusChest = [bonusChest];

    // 2. Levels
    const levelsArr = [];
    document.querySelectorAll('.bp-level-card').forEach(card => {
        const lvl = { $: {} };
        card.querySelectorAll('.bp-l-attr').forEach(el => lvl.$[el.dataset.attr] = el.value);
        card.querySelectorAll('.bp-reward-row').forEach(tr => {
            const tag = tr.dataset.tag;
            if (!lvl[tag]) lvl[tag] = [];
            const r = { $: {} };
            tr.querySelectorAll('.bp-r-attr').forEach(el => r.$[el.dataset.attr] = el.value);
            lvl[tag].push(r);
        });
        levelsArr.push(lvl);
    });
    root.Levels = [{ Level: levelsArr }];

    // 3. Tasks
    const tasksArr = [];
    document.querySelectorAll('.bp-task-card').forEach(card => {
        const t = { $: {} };
        card.querySelectorAll('.bp-t-attr').forEach(el => t.$[el.dataset.attr] = el.value);
        tasksArr.push(t);
    });
    root.Tasks = [{ Task: tasksArr }];

    // 4. Bundles
    const bundlesArr = [];
    document.querySelectorAll('.bp-bundle-card').forEach(card => {
        const b = { $: {} };
        card.querySelectorAll('.bp-b-attr').forEach(el => b.$[el.dataset.attr] = el.value);
        card.querySelectorAll('.bp-reward-row').forEach(tr => {
            const tag = tr.dataset.tag;
            if (!b[tag]) b[tag] = [];
            const r = { $: {} };
            tr.querySelectorAll('.bp-r-attr').forEach(el => r.$[el.dataset.attr] = el.value);
            b[tag].push(r);
        });
        bundlesArr.push(b);
    });
    root.Bundles = [{ Bundle: bundlesArr }];

    // Set export state
    battlePassData.isExport = getExportState();

    try {
        const response = await fetch('/api/battle-pass', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(battlePassData)
        });
        
        let result = {};
        try {
            result = await response.json();
        } catch(e) {
            throw new Error(await response.text());
        }

        if (result.success) {
            showAlert('Lưu cấu hình Battle Pass thành công!', 'success');
        } else {
            showAlert('Lưu thất bại: ' + result.error, 'danger');
        }
    } catch (err) {
        showAlert('Lưu thất bại: ' + err.message, 'danger');
    }
}

// ----------------------------------------------------
// TÍCH NẠP LIÊN TIẾP (RechargeEvent_OldServer.xml)
// ----------------------------------------------------
let rechargeOldServerData = null;

async function loadRechargeOldServer() {
    try {
        const endpoints = [
            { url: '/api/recharge-old-server' },
            { url: '/api/general/script-items' }
        ];
        const responses = await Promise.all(endpoints.map(e => fetch(e.url)));

        rechargeOldServerData = await responses[0].json();
        scriptItemsGeneral = await responses[1].json();

        renderRechargeXmlForm(rechargeOldServerData);
    } catch (error) {
        console.error(error);
        document.getElementById('recharge-xml-form').innerHTML = '<p class="text-danger">Không thể tải dữ liệu.</p>';
    }
}

function renderRechargeXmlForm(data) {
    const container = document.getElementById('recharge-xml-form');
    if (!data || !data.RechargeEvent || !data.RechargeEvent.RechargeMilestone) {
        container.innerHTML = '<p class="text-muted">Không có dữ liệu mốc nạp.</p>';
        return;
    }

    let milestones = data.RechargeEvent.RechargeMilestone;
    if (!Array.isArray(milestones)) milestones = [milestones];

    let html = '<div class="accordion" id="rechargeOldServerAccordion">';

    milestones.forEach((m, mIdx) => {
        let packages = m.Package || [];
        if (!Array.isArray(packages)) packages = [packages];

        html += `
        <div class="accordion-item mb-3 border border-secondary shadow-sm">
            <h2 class="accordion-header" id="heading-${mIdx}">
                <button class="accordion-button ${mIdx === 0 ? '' : 'collapsed'} bg-light fw-bold" type="button" data-bs-toggle="collapse" data-bs-target="#collapse-${mIdx}" aria-expanded="${mIdx === 0 ? 'true' : 'false'}" aria-controls="collapse-${mIdx}">
                    <i class="bi bi-gift-fill text-danger me-2"></i> Mốc Nạp: ${m.$?.Name || 'Unknown'} (Giá: ${m.$?.Price || 0} KNB)
                </button>
            </h2>
            <div id="collapse-${mIdx}" class="accordion-collapse collapse ${mIdx === 0 ? 'show' : ''}" aria-labelledby="heading-${mIdx}" data-bs-parent="#rechargeOldServerAccordion">
                <div class="accordion-body">
                    <div class="row g-2 mb-3">
                        <div class="col-md-4">
                            <label class="small fw-bold">Tên Mốc (Name)</label>
                            <input type="text" class="form-control form-control-sm recharge-m-attr" data-idx="${mIdx}" data-attr="Name" value="${m.$?.Name || ''}">
                        </div>
                        <div class="col-md-4">
                            <label class="small fw-bold">Giá Nạp (Price - KNB)</label>
                            <input type="number" class="form-control form-control-sm recharge-m-attr" data-idx="${mIdx}" data-attr="Price" value="${m.$?.Price || 0}">
                        </div>
                        <div class="col-md-4 text-end d-flex align-items-end justify-content-end">
                            <button class="btn btn-sm btn-outline-danger" onclick="deleteRechargeMilestone(${mIdx})"><i class="bi bi-trash"></i> Xóa Mốc Này</button>
                        </div>
                    </div>

                    <h6 class="border-bottom pb-1 mb-2 text-primary">Chi Tiết Ngày Nhận Quà (Package)</h6>
                    <div class="table-responsive">
                        <table class="table table-bordered table-sm align-middle text-center mb-0">
                            <thead class="table-light">
                                <tr>
        `;
        
        // Headers for packages (Ngày 1 to Ngày 6)
        packages.forEach((pkg, pIdx) => {
            html += `<th>${pkg.$?.Name || 'Ngày '+(pIdx+1)}</th>`;
        });
        
        html += `
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
        `;

        // Columns for items
        packages.forEach((pkg, pIdx) => {
            let items = pkg.Item || [];
            if (!Array.isArray(items)) items = [items];

            html += `<td style="vertical-align: top; text-align: left; min-width: 200px;">
                        <input type="hidden" class="recharge-p-attr" data-midx="${mIdx}" data-pidx="${pIdx}" data-attr="ID" value="${pkg.$?.ID || (pIdx+1)}">
                        <input type="hidden" class="recharge-p-attr" data-midx="${mIdx}" data-pidx="${pIdx}" data-attr="Name" value="${pkg.$?.Name || 'Ngày '+(pIdx+1)}">
                        <div class="d-flex justify-content-between mb-1">
                            <span class="small fw-bold text-muted">Vật phẩm</span>
                            <button class="btn btn-link p-0 text-success m-0" onclick="addRechargePackageItem(${mIdx}, ${pIdx})" title="Thêm vật phẩm"><i class="bi bi-plus-circle-fill"></i></button>
                        </div>
            `;
            
            items.forEach((item, iIdx) => {
                const itemName = scriptItemsGeneral.find(si => si.ItemID == item.$?.ID)?.Name || 'Unknown';
                html += `
                    <div class="input-group input-group-sm mb-1 recharge-item-row" data-midx="${mIdx}" data-pidx="${pIdx}" data-iidx="${iIdx}">
                        <input type="number" class="form-control recharge-i-id" value="${item.$?.ID || 0}" style="max-width: 70px;" title="${itemName}">
                        <input type="number" class="form-control recharge-i-count" value="${item.$?.Count || 1}" style="max-width: 60px;">
                        <button class="btn btn-outline-danger px-2 py-0" onclick="deleteRechargePackageItem(${mIdx}, ${pIdx}, ${iIdx})"><i class="bi bi-x"></i></button>
                    </div>
                `;
            });

            html += `</td>`;
        });

        html += `
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>
        `;
    });

    html += '</div>';

    if (milestones.length === 0) {
        html = '<p class="text-muted p-3">Chưa có mốc nạp nào.</p>';
    }

    container.innerHTML = html;
}

function gatherRechargeXmlData() {
    if (!rechargeOldServerData || !rechargeOldServerData.RechargeEvent) return;
    let milestones = rechargeOldServerData.RechargeEvent.RechargeMilestone;
    if (!Array.isArray(milestones)) {
        milestones = [milestones];
        rechargeOldServerData.RechargeEvent.RechargeMilestone = milestones;
    }

    // Update milestone attributes
    document.querySelectorAll('.recharge-m-attr').forEach(input => {
        const midx = input.dataset.idx;
        const attr = input.dataset.attr;
        if (!milestones[midx]) return;
        if (!milestones[midx].$) milestones[midx].$ = {};
        milestones[midx].$[attr] = input.value;
    });

    // Gather item counts and IDs
    document.querySelectorAll('.recharge-item-row').forEach(row => {
        const midx = row.dataset.midx;
        const pidx = row.dataset.pidx;
        const iidx = row.dataset.iidx;

        if (!milestones[midx]) return;
        let packages = milestones[midx].Package;
        if (!Array.isArray(packages)) packages = [packages];
        
        if (!packages[pidx]) return;
        let items = packages[pidx].Item;
        if (!Array.isArray(items)) items = [items];

        if (!items[iidx]) return;
        if (!items[iidx].$) items[iidx].$ = {};

        items[iidx].$.ID = row.querySelector('.recharge-i-id').value;
        items[iidx].$.Count = row.querySelector('.recharge-i-count').value;
        
        packages[pidx].Item = items;
        milestones[midx].Package = packages;
    });
}

function addRechargeMilestone() {
    if (!rechargeOldServerData) return;
    gatherRechargeXmlData();

    if (!rechargeOldServerData.RechargeEvent) rechargeOldServerData.RechargeEvent = {};
    if (!rechargeOldServerData.RechargeEvent.RechargeMilestone) rechargeOldServerData.RechargeEvent.RechargeMilestone = [];
    let milestones = rechargeOldServerData.RechargeEvent.RechargeMilestone;
    if (!Array.isArray(milestones)) {
        milestones = [milestones];
        rechargeOldServerData.RechargeEvent.RechargeMilestone = milestones;
    }

    const newPackages = [];
    for (let i = 1; i <= 6; i++) {
        newPackages.push({
            $: { ID: i.toString(), Name: "Ngày " + i },
            Item: [ { $: { ID: "506", Count: "10" } } ]
        });
    }

    milestones.push({
        $: { Name: "New KNB", Price: "100" },
        Package: newPackages
    });

    renderRechargeXmlForm(rechargeOldServerData);
}

function deleteRechargeMilestone(midx) {
    if (!confirm('Xóa mốc nạp này?')) return;
    gatherRechargeXmlData();
    rechargeOldServerData.RechargeEvent.RechargeMilestone.splice(midx, 1);
    renderRechargeXmlForm(rechargeOldServerData);
}

function addRechargePackageItem(midx, pidx) {
    gatherRechargeXmlData();
    let milestones = rechargeOldServerData.RechargeEvent.RechargeMilestone;
    if (!Array.isArray(milestones)) milestones = [milestones];
    let packages = milestones[midx].Package;
    if (!Array.isArray(packages)) packages = [packages];
    let items = packages[pidx].Item;
    if (!Array.isArray(items)) items = [items];
    
    items.push({ $: { ID: "0", Count: "1" } });
    packages[pidx].Item = items;
    milestones[midx].Package = packages;
    
    renderRechargeXmlForm(rechargeOldServerData);
}

function deleteRechargePackageItem(midx, pidx, iidx) {
    gatherRechargeXmlData();
    let milestones = rechargeOldServerData.RechargeEvent.RechargeMilestone;
    if (!Array.isArray(milestones)) milestones = [milestones];
    let packages = milestones[midx].Package;
    if (!Array.isArray(packages)) packages = [packages];
    let items = packages[pidx].Item;
    if (!Array.isArray(items)) items = [items];
    
    items.splice(iidx, 1);
    packages[pidx].Item = items;
    milestones[midx].Package = packages;

    renderRechargeXmlForm(rechargeOldServerData);
}

async function saveRechargeXml() {
    if (!rechargeOldServerData) return;
    gatherRechargeXmlData();
    rechargeOldServerData.isExport = getExportState();

    try {
        const response = await fetch('/api/recharge-old-server', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(rechargeOldServerData)
        });
        if (response.ok) {
            showAlert('Lưu cấu hình Tích Nạp Liên Tiếp thành công!', 'success');
        } else {
            throw new Error(await response.text());
        }
    } catch (err) {
        showAlert('Lỗi: ' + err.message, 'danger');
    }
}

