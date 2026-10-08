const express = require('express');
const cors = require('cors');
const path = require('path');
const fs = require('fs');
const xml2js = require('xml2js');
require('dotenv').config();

const app = express();
const PORT = process.env.PORT || 3000;

// Middleware
app.use(cors());
app.use(express.json());
app.use(express.static(path.join(__dirname, 'public')));

// Use dynamic path from .env or default to relative path
const kv1ConfigPath = process.env.KV1_CONFIG_PATH 
    ? path.resolve(process.env.KV1_CONFIG_PATH)
    : path.resolve(__dirname, '../../game-config/Config/Config');

// Utility to read XML
async function readXml(filePath) {
    if (!fs.existsSync(filePath)) {
        throw new Error(`File not found: ${filePath}`);
    }
    const xmlContent = fs.readFileSync(filePath, 'utf-8');
    const parser = new xml2js.Parser();
    return await parser.parseStringPromise(xmlContent);
}

// Utility to write XML
async function writeXml(filePath, jsonObj) {
    const builder = new xml2js.Builder();
    const xmlContent = builder.buildObject(jsonObj);
    fs.writeFileSync(filePath, xmlContent, 'utf-8');
}

// Special Utility for LuckySpin to preserve comments
async function writeLuckySpinXml(filePath, jsonObj, originalPath) {
    const builder = new xml2js.Builder({
        cdata: true, // Preserve CDATA for Rule
        renderOpts: { 'pretty': true, 'indent': '    ', 'newline': '\n' }
    });
    let xmlContent = builder.buildObject(jsonObj);

    // Read original to extract comments
    if (fs.existsSync(originalPath)) {
        const original = fs.readFileSync(originalPath, 'utf-8');
        
        // 1. Top description comment
        const topMatch = original.match(/<!--[\s\S]*?TotalSpaceNeed[\s\S]*?-->/);
        if (topMatch) {
            xmlContent = xmlContent.replace(/<VongQuayMayMan/, topMatch[0] + '\n<VongQuayMayMan');
        }

        // 2. Fees comment
        const feesMatch = original.match(/<!--Fees[\s\S]*?-->/);
        if (feesMatch) {
            xmlContent = xmlContent.replace('<Fees>', feesMatch[0] + '\n        <Fees>');
        }

        // 3. Rates comment
        const ratesMatch = original.match(/<!-- Rate[\s\S]*?-->/);
        if (ratesMatch) {
            xmlContent = xmlContent.replace('<Rates>', ratesMatch[0] + '\n        <Rates>');
        }

        // 4. Accumulate comment
        const accMatch = original.match(/<!-- Accumulate[\s\S]*?-->/);
        if (accMatch) {
            xmlContent = xmlContent.replace('<Accumulate>', accMatch[0] + '\n        <Accumulate>');
        }

        // 5. TopReward comment
        const topRMatch = original.match(/<!--TopReward[\s\S]*?-->/);
        if (topRMatch) {
            xmlContent = xmlContent.replace('<TopReward>', topRMatch[0] + '\n        <TopReward>');
        }
    }

    fs.writeFileSync(filePath, xmlContent, 'utf-8');
}

// Special Utility for ServerMergeRewards to preserve comments
async function writeServerMergeXml(filePath, jsonObj, originalPath) {
    const builder = new xml2js.Builder({
        renderOpts: { 'pretty': true, 'indent': '    ', 'newline': '\n' }
    });
    let xmlContent = builder.buildObject(jsonObj);

    if (fs.existsSync(originalPath)) {
        const original = fs.readFileSync(originalPath, 'utf-8');
        const commentMatch = original.match(/<!--[\s\S]*?-->/);
        if (commentMatch) {
            xmlContent = xmlContent.replace(/(<MergeRewards[^>]*>)/, '$1\n    ' + commentMatch[0]);
        }
    }

    fs.writeFileSync(filePath, xmlContent, 'utf-8');
}

// Special Utility for BattlePass to preserve comments
async function writeBattlePassXml(filePath, jsonObj, originalPath) {
    const builder = new xml2js.Builder({
        renderOpts: { 'pretty': true, 'indent': '    ', 'newline': '\n' }
    });
    let xmlContent = builder.buildObject(jsonObj);

    if (fs.existsSync(originalPath)) {
        const original = fs.readFileSync(originalPath, 'utf-8');
        const commentMatch = original.match(/<!--[\s\S]*?-->/);
        if (commentMatch) {
            xmlContent = xmlContent.replace(/(<BattlePass[^>]*>)/, '\n' + commentMatch[0] + '\n\n$1');
        }
    }

    fs.writeFileSync(filePath, xmlContent, 'utf-8');
}

// Global Event Config Route
app.get('/api/events-time-config', async (req, res) => {
    try {
        const filePath = path.join(kv1ConfigPath, 'KT_Interactive/EventsTimeConfig.xml');
        const data = await readXml(filePath);
        res.json(data);
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post('/api/events-time-config', async (req, res) => {
    try {
        const { isExport, ...newJsonObj } = req.body;
        const targetPath = isExport 
            ? path.join(__dirname, 'exports', 'EventsTimeConfig.xml')
            : path.join(kv1ConfigPath, 'KT_Interactive/EventsTimeConfig.xml');
        
        await writeXml(targetPath, newJsonObj);
        res.json({ success: true, message: `EventsTimeConfig.xml ${isExport ? 'exported to /exports' : 'updated'} successfully` });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.get('/api/monthly-compose', async (req, res) => {
    try {
        const filePath = path.join(kv1ConfigPath, 'KT_GameEvents/MonthlyCompose.json');
        const data = fs.readFileSync(filePath, 'utf-8');
        res.json(JSON.parse(data));
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post('/api/monthly-compose', async (req, res) => {
    try {
        const { isExport, ...newJsonObj } = req.body;
        const targetPath = isExport 
            ? path.join(__dirname, 'exports', 'MonthlyCompose.json')
            : path.join(kv1ConfigPath, 'KT_GameEvents/MonthlyCompose.json');
            
        fs.writeFileSync(targetPath, JSON.stringify(newJsonObj, null, 2), 'utf-8');
        res.json({ success: true, message: 'MonthlyCompose.json updated successfully' });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// RechargeEvent_OldServer (Tích Nạp Liên Tiếp)
app.get('/api/recharge-old-server', async (req, res) => {
    try {
        const filePath = path.join(kv1ConfigPath, 'KT_Interactive/RechargeEvent_OldServer.xml');
        const data = await readXml(filePath);
        res.json(data);
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post('/api/recharge-old-server', async (req, res) => {
    try {
        const { isExport, ...newJsonObj } = req.body;
        const targetPath = isExport 
            ? path.join(__dirname, 'exports', 'RechargeEvent_OldServer.xml')
            : path.join(kv1ConfigPath, 'KT_Interactive/RechargeEvent_OldServer.xml');
            
        await writeXml(targetPath, newJsonObj);
        res.json({ success: true, message: 'RechargeEvent_OldServer.xml updated successfully' });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// Slot Machine XML
app.get('/api/slot-machine', async (req, res) => {
    try {
        const filePath = path.join(kv1ConfigPath, 'KT_Interactive/SlotMachine.xml');
        const data = await readXml(filePath);
        res.json(data);
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post('/api/slot-machine', async (req, res) => {
    try {
        const { isExport, ...newJsonObj } = req.body;
        const targetPath = isExport 
            ? path.join(__dirname, 'exports', 'SlotMachine.xml')
            : path.join(kv1ConfigPath, 'KT_Interactive/SlotMachine.xml');
            
        await writeXml(targetPath, newJsonObj);
        res.json({ success: true });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// Recharge JSON (script_items_recharge.json)
app.get('/api/recharge/script-items', (req, res) => {
    try {
        const filePath = path.join(kv1ConfigPath, 'KT_Item/Other/script_items_recharge.json');
        const data = fs.readFileSync(filePath, 'utf-8');
        res.json(JSON.parse(data));
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post('/api/recharge/script-items', (req, res) => {
    try {
        const { isExport, data } = req.body;
        const targetPath = isExport 
            ? path.join(__dirname, 'exports', 'script_items_recharge.json')
            : path.join(kv1ConfigPath, 'KT_Item/Other/script_items_recharge.json');
        fs.writeFileSync(targetPath, JSON.stringify(data || req.body, null, 4), 'utf-8');
        res.json({ success: true });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// Randombox Packages
app.get('/api/recharge/randomboxes-packages', (req, res) => {
    try {
        const filePath = path.join(kv1ConfigPath, 'KT_Item/RandomBox/randomboxes_tichxu_packages.json');
        const data = fs.readFileSync(filePath, 'utf-8');
        res.json(JSON.parse(data));
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post('/api/recharge/randomboxes-packages', (req, res) => {
    try {
        const { isExport, data } = req.body;
        const targetPath = isExport 
            ? path.join(__dirname, 'exports', 'randomboxes_tichxu_packages.json')
            : path.join(kv1ConfigPath, 'KT_Item/RandomBox/randomboxes_tichxu_packages.json');
        fs.writeFileSync(targetPath, JSON.stringify(data || req.body, null, 4), 'utf-8');
        res.json({ success: true });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// Randombox Milestones
app.get('/api/recharge/randomboxes-milestones', (req, res) => {
    try {
        const filePath = path.join(kv1ConfigPath, 'KT_Item/RandomBox/randomboxes_tichxu_milestones.json');
        const data = fs.readFileSync(filePath, 'utf-8');
        res.json(JSON.parse(data));
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post('/api/recharge/randomboxes-milestones', (req, res) => {
    try {
        const { isExport, data } = req.body;
        const targetPath = isExport 
            ? path.join(__dirname, 'exports', 'randomboxes_tichxu_milestones.json')
            : path.join(kv1ConfigPath, 'KT_Item/RandomBox/randomboxes_tichxu_milestones.json');
        fs.writeFileSync(targetPath, JSON.stringify(data || req.body, null, 4), 'utf-8');
        res.json({ success: true });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// Recharge XML
app.get('/api/recharge-xml', async (req, res) => {
    try {
        const filePath = path.join(kv1ConfigPath, 'KT_Interactive/RechargeEvent_OldServer.xml');
        if (fs.existsSync(filePath)) {
             const data = await readXml(filePath);
             res.json(data);
        } else {
             res.json({}); // Provide empty if not found
        }
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post('/api/recharge-xml', async (req, res) => {
    try {
        const { isExport, ...newJsonObj } = req.body;
        const targetPath = isExport 
            ? path.join(__dirname, 'exports', 'RechargeEvent_OldServer.xml')
            : path.join(kv1ConfigPath, 'KT_Interactive/RechargeEvent_OldServer.xml');
            
        await writeXml(targetPath, newJsonObj);
        res.json({ success: true });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// CheckIn XML
app.get('/api/checkin', async (req, res) => {
    try {
        const filePath = path.join(kv1ConfigPath, 'KT_Interactive/CheckIn.xml');
        const data = await readXml(filePath);
        res.json(data);
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post('/api/checkin', async (req, res) => {
    try {
        const { isExport, ...newJsonObj } = req.body;
        const targetPath = isExport 
            ? path.join(__dirname, 'exports', 'CheckIn.xml')
            : path.join(kv1ConfigPath, 'KT_Interactive/CheckIn.xml');
            
        await writeXml(targetPath, newJsonObj);
        res.json({ success: true });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// SeashellCircle (Bách Bảo Rương)
app.get('/api/seashell', async (req, res) => {
    try {
        const filePath = path.join(kv1ConfigPath, 'KT_Activity/KTSeashellCircle-EVENT.xml');
        const data = await readXml(filePath);
        res.json(data);
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post('/api/seashell', async (req, res) => {
    try {
        const { isExport, ...newJsonObj } = req.body;
        const targetPath = isExport 
            ? path.join(__dirname, 'exports', 'KTSeashellCircle-EVENT.xml')
            : path.join(kv1ConfigPath, 'KT_Activity/KTSeashellCircle-EVENT.xml');
            
        await writeXml(targetPath, newJsonObj);
        res.json({ success: true });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// FlowerTower (Thiên Tầng Tháp)
app.get('/api/flowertower', async (req, res) => {
    try {
        const filePath = path.join(kv1ConfigPath, 'KT_Interactive/FlowerTower.xml');
        const data = await readXml(filePath);
        res.json(data);
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post('/api/flowertower', async (req, res) => {
    try {
        const { isExport, ...newJsonObj } = req.body;
        const targetPath = isExport 
            ? path.join(__dirname, 'exports', 'FlowerTower.xml')
            : path.join(kv1ConfigPath, 'KT_Interactive/FlowerTower.xml');
            
        await writeXml(targetPath, newJsonObj);
        res.json({ success: true });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// Flower Tower Rewards JSON (script_items_flowertower.json)
app.get('/api/flowertower/script-items', (req, res) => {
    try {
        const filePath = path.join(kv1ConfigPath, 'KT_Item/Other/script_items_flowertower.json');
        const data = fs.readFileSync(filePath, 'utf-8');
        res.json(JSON.parse(data));
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post('/api/flowertower/script-items', (req, res) => {
    try {
        const { isExport, data } = req.body;
        const targetPath = isExport 
            ? path.join(__dirname, 'exports', 'script_items_flowertower.json')
            : path.join(kv1ConfigPath, 'KT_Item/Other/script_items_flowertower.json');
        fs.writeFileSync(targetPath, JSON.stringify(data || req.body, null, 4), 'utf-8');
        res.json({ success: true });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// Flower Tower Randomboxes (randomboxes_flowertower.json)
app.get('/api/flowertower/randomboxes', (req, res) => {
    try {
        const filePath = path.join(kv1ConfigPath, 'KT_Item/RandomBox/randomboxes_flowertower.json');
        const data = fs.readFileSync(filePath, 'utf-8');
        res.json(JSON.parse(data));
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post('/api/flowertower/randomboxes', (req, res) => {
    try {
        const { isExport, data } = req.body;
        const targetPath = isExport 
            ? path.join(__dirname, 'exports', 'randomboxes_flowertower.json')
            : path.join(kv1ConfigPath, 'KT_Item/RandomBox/randomboxes_flowertower.json');
        fs.writeFileSync(targetPath, JSON.stringify(data || req.body, null, 4), 'utf-8');
        res.json({ success: true });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// UuDaiDisplay
app.get('/api/uudai-display', async (req, res) => {
    try {
        const filePath = path.join(kv1ConfigPath, 'KT_Interactive/UuDaiDisplay.xml');
        const data = await readXml(filePath);
        res.json(data);
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post('/api/uudai-display', async (req, res) => {
    try {
        const { isExport, ...newJsonObj } = req.body;
        const targetPath = isExport 
            ? path.join(__dirname, 'exports', 'UuDaiDisplay.xml')
            : path.join(kv1ConfigPath, 'KT_Interactive/UuDaiDisplay.xml');
            
        await writeXml(targetPath, newJsonObj);
        res.json({ success: true });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// UuDaiDisplay - New Server
app.get('/api/uudai-display-newserver', async (req, res) => {
    try {
        const filePath = path.join(kv1ConfigPath, 'KT_Interactive/UuDaiDisplay_NewServer.xml');
        const data = await readXml(filePath);
        res.json(data);
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post('/api/uudai-display-newserver', async (req, res) => {
    try {
        const { isExport, ...newJsonObj } = req.body;
        const targetPath = isExport 
            ? path.join(__dirname, 'exports', 'UuDaiDisplay_NewServer.xml')
            : path.join(kv1ConfigPath, 'KT_Interactive/UuDaiDisplay_NewServer.xml');
            
        await writeXml(targetPath, newJsonObj);
        res.json({ success: true });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});


// RechargeAward (DayRecharge, AccumulateRecharge)
app.get('/api/recharge-award', async (req, res) => {
    try {
        const filePath = path.join(kv1ConfigPath, 'RechargeAward.xml');
        const data = await readXml(filePath);
        res.json(data);
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post('/api/recharge-award', async (req, res) => {
    try {
        const { isExport, ...newJsonObj } = req.body;
        const targetPath = isExport 
            ? path.join(__dirname, 'exports', 'RechargeAward.xml')
            : path.join(kv1ConfigPath, 'RechargeAward.xml');
            
        await writeXml(targetPath, newJsonObj);
        res.json({ success: true });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// EventCompose XML
app.get('/api/event-compose-xml', async (req, res) => {
    try {
        const filePath = path.join(kv1ConfigPath, 'KT_Interactive/EventCompose.xml');
        const data = await readXml(filePath);
        res.json(data);
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post('/api/event-compose-xml', async (req, res) => {
    try {
        const { isExport, ...newJsonObj } = req.body;
        const targetPath = isExport 
            ? path.join(__dirname, 'exports', 'EventCompose.xml')
            : path.join(kv1ConfigPath, 'KT_Interactive/EventCompose.xml');
            
        await writeXml(targetPath, newJsonObj);
        res.json({ success: true });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// ExchangeTicket XML
app.get('/api/exchange-ticket', async (req, res) => {
    try {
        const filePath = path.join(kv1ConfigPath, 'KT_Interactive/ExchangeTicketEvent.xml');
        const data = await readXml(filePath);
        res.json(data);
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post('/api/exchange-ticket', async (req, res) => {
    try {
        const { isExport, ...newJsonObj } = req.body;
        const targetPath = isExport 
            ? path.join(__dirname, 'exports', 'ExchangeTicketEvent.xml')
            : path.join(kv1ConfigPath, 'KT_Interactive/ExchangeTicketEvent.xml');
            
        await writeXml(targetPath, newJsonObj);
        res.json({ success: true });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// LuckySpin (Tân Niên Bí Bảo)
app.get('/api/lucky-spin', async (req, res) => {
    try {
        const filePath = path.join(kv1ConfigPath, 'KT_Interactive/VongQuayMayMan.xml');
        const data = await readXml(filePath);
        res.json(data);
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post('/api/lucky-spin', async (req, res) => {
    try {
        const { isExport, ...newJsonObj } = req.body;
        const originalPath = path.join(kv1ConfigPath, 'KT_Interactive/VongQuayMayMan.xml');
        const targetPath = isExport 
            ? path.join(__dirname, 'exports', 'VongQuayMayMan.xml')
            : originalPath;
            
        await writeLuckySpinXml(targetPath, newJsonObj, originalPath);
        res.json({ success: true });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// Server Merge Rewards
app.get('/api/server-merge', async (req, res) => {
    try {
        const filePath = path.join(kv1ConfigPath, 'KT_Interactive/ServerMergeRewards.xml');
        const data = await readXml(filePath);
        res.json(data);
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post('/api/server-merge', async (req, res) => {
    try {
        const { isExport, ...newJsonObj } = req.body;
        const originalPath = path.join(kv1ConfigPath, 'KT_Interactive/ServerMergeRewards.xml');
        const targetPath = isExport 
            ? path.join(__dirname, 'exports', 'ServerMergeRewards.xml')
            : originalPath;
            
        await writeServerMergeXml(targetPath, newJsonObj, originalPath);
        res.json({ success: true });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// Battle Pass Season
app.get('/api/battle-pass', async (req, res) => {
    try {
        const filePath = path.join(kv1ConfigPath, 'KT_Interactive/BattlePassSeason.xml');
        const data = await readXml(filePath);
        res.json(data);
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post('/api/battle-pass', async (req, res) => {
    try {
        const { isExport, ...newJsonObj } = req.body;
        const originalPath = path.join(kv1ConfigPath, 'KT_Interactive/BattlePassSeason.xml');
        const targetPath = isExport 
            ? path.join(__dirname, 'exports', 'BattlePassSeason.xml')
            : originalPath;
            
        await writeBattlePassXml(targetPath, newJsonObj, originalPath);
        res.json({ success: true });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// General JSON Item/Box configs
app.get('/api/general/script-items', (req, res) => {
    try {
        const filePath = path.join(kv1ConfigPath, 'KT_Item/Other/script_items.json');
        const data = fs.readFileSync(filePath, 'utf-8');
        res.json(JSON.parse(data));
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post('/api/general/script-items', (req, res) => {
    try {
        const { isExport, data } = req.body;
        const targetPath = isExport 
            ? path.join(__dirname, 'exports', 'script_items.json')
            : path.join(kv1ConfigPath, 'KT_Item/Other/script_items.json');
        fs.writeFileSync(targetPath, JSON.stringify(data || req.body, null, 4), 'utf-8');
        res.json({ success: true });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.get('/api/general/randomboxes', (req, res) => {
    try {
        const filePath = path.join(kv1ConfigPath, 'KT_Item/randomboxes.json');
        const data = fs.readFileSync(filePath, 'utf-8');
        res.json(JSON.parse(data));
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

app.post('/api/general/randomboxes', (req, res) => {
    try {
        const { isExport, data } = req.body;
        const targetPath = isExport 
            ? path.join(__dirname, 'exports', 'randomboxes.json')
            : path.join(kv1ConfigPath, 'KT_Item/randomboxes.json');
        fs.writeFileSync(targetPath, JSON.stringify(data || req.body, null, 4), 'utf-8');
        res.json({ success: true });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});


app.listen(PORT, () => {
    console.log(`Event Configurator Server running at http://localhost:${PORT}`);
    console.log(`Watching game config at: ${kv1ConfigPath}`);
});
