-- ==============================================================================
-- MASA Accounting Management System - Egyptian Localization Seed Data
-- ==============================================================================

-- 1. Default Company (Egyptian Enterprise)
INSERT INTO companies (id, code, name, legal_name, tax_number, registration_number, base_currency, fiscal_year_start_month, fiscal_year_start_day, address, city, country, phone, email)
VALUES (
    '00000000-0000-0000-0000-000000000001', 
    'MASA-EG', 
    'MASA Egypt Enterprise S.A.E.', 
    'MASA Trading & Software Solutions S.A.E.', 
    '692-481-930', 
    'CR-109482', 
    'EGP', 
    1, 
    1, 
    'Plot 45, North 90th Street, 5th Settlement, New Cairo', 
    'Cairo', 
    'Egypt', 
    '+20 2 2810 5400', 
    'contact@masa-egypt.com'
)
ON CONFLICT (code) DO NOTHING;

-- 2. Default Roles
INSERT INTO roles (id, company_id, name, description, is_system_role)
VALUES 
('10000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 'Administrator', 'Full system and administrative access', TRUE),
('10000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', 'Chief Accountant', 'Journal entries, posting, trial balance, tax audit', TRUE),
('10000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000001', 'Sales Specialist', 'Sales e-invoices, clients, receipts', TRUE),
('10000000-0000-0000-0000-000000000004', '00000000-0000-0000-0000-000000000001', 'Procurement Officer', 'Vendor bills, suppliers, warehouse receipts', TRUE)
ON CONFLICT (company_id, name) DO NOTHING;

-- 3. Default Admin User (Password: Admin@123)
INSERT INTO app_users (id, company_id, role_id, username, email, password_hash, full_name, phone, is_active)
VALUES (
    '20000000-0000-0000-0000-000000000001',
    '00000000-0000-0000-0000-000000000001',
    '10000000-0000-0000-0000-000000000001',
    'admin',
    'admin@masa-egypt.com',
    '$2a$11$qRzN7HkUf9k5P8N3FkWbje4y1KzQ0PZZi3k.bT9E4E2H0Z7h3lBvG',
    'Ahmed Mostafa El-Sayed',
    '+20 100 123 4567',
    TRUE
)
ON CONFLICT (username) DO NOTHING;

-- 4. Currencies (EGP as Base Currency)
INSERT INTO currencies (code, name, symbol, decimal_places, exchange_rate, is_base, is_active)
VALUES 
('EGP', 'Egyptian Pound', 'EGP', 2, 1.000000, TRUE, TRUE),
('USD', 'US Dollar', '$', 2, 48.750000, FALSE, TRUE),
('EUR', 'Euro', '€', 2, 53.200000, FALSE, TRUE),
('SAR', 'Saudi Riyal', 'SAR', 2, 13.000000, FALSE, TRUE),
('AED', 'UAE Dirham', 'AED', 2, 13.270000, FALSE, TRUE)
ON CONFLICT (code) DO NOTHING;

-- 5. Egyptian Tax Law System (ETA Compliance)
-- 14% Standard VAT (قيمة مضافة), 1% WHT (خصم وأرباح تجارية وصناعية), 0% Export/Exempt
INSERT INTO taxes (id, company_id, code, name, rate, tax_type, is_active)
VALUES 
('30000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 'VAT-14', 'Standard Egyptian VAT (14%)', 0.1400, 'Both', TRUE),
('30000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', 'WHT-1', 'Withholding Tax - Commercial (1%)', 0.0100, 'Both', TRUE),
('30000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000001', 'VAT-0', 'Zero-Rated / Export (0%)', 0.0000, 'Both', TRUE),
('30000000-0000-0000-0000-000000000004', '00000000-0000-0000-0000-000000000001', 'WHT-3', 'Withholding Tax - Services (3%)', 0.0300, 'Both', TRUE)
ON CONFLICT (company_id, code) DO NOTHING;

-- 6. Account Categories
INSERT INTO account_categories (id, name, category_type, normal_balance, display_order)
VALUES 
('40000000-0000-0000-0000-000000000001', 'Current Assets', 'Asset', 'Debit', 1),
('40000000-0000-0000-0000-000000000002', 'Fixed & Non-Current Assets', 'Asset', 'Debit', 2),
('40000000-0000-0000-0000-000000000003', 'Current Liabilities', 'Liability', 'Credit', 3),
('40000000-0000-0000-0000-000000000004', 'Long-Term Liabilities', 'Liability', 'Credit', 4),
('40000000-0000-0000-0000-000000000005', 'Equity & Retained Earnings', 'Equity', 'Credit', 5),
('40000000-0000-0000-0000-000000000006', 'Operating Sales Revenues', 'Revenue', 'Credit', 6),
('40000000-0000-0000-0000-000000000007', 'Cost of Goods Sold (COGS)', 'Expense', 'Debit', 7),
('40000000-0000-0000-0000-000000000008', 'Selling & General Expenses (SG&A)', 'Expense', 'Debit', 8),
('40000000-0000-0000-0000-000000000009', 'Tax & Financing Expenses', 'Expense', 'Debit', 9)
ON CONFLICT DO NOTHING;

-- 7. Standard Egyptian Chart of Accounts (EAS Compliant)
INSERT INTO accounts (id, company_id, category_id, account_code, account_name, description, currency, is_header, is_active)
VALUES 
-- Current Assets (1100)
('50000000-0000-0000-0000-000000000101', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000001', '1010', 'Main Cash Box (Petty Cash EGP)', 'Treasury main safe & cash desk', 'EGP', FALSE, TRUE),
('50000000-0000-0000-0000-000000000102', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000001', '1020', 'CIB Operating Bank Account (EGP)', 'Commercial International Bank - Current Account', 'EGP', FALSE, TRUE),
('50000000-0000-0000-0000-000000000103', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000001', '1025', 'NBE Bank Account (EGP)', 'National Bank of Egypt - Operations Account', 'EGP', FALSE, TRUE),
('50000000-0000-0000-0000-000000000104', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000001', '1200', 'Trade Accounts Receivable (Clients)', 'Egyptian commercial clients ledger', 'EGP', FALSE, TRUE),
('50000000-0000-0000-0000-000000000105', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000001', '1300', 'Finished Goods Merchandise Inventory', 'Warehouse stock valuation at cost', 'EGP', FALSE, TRUE),
('50000000-0000-0000-0000-000000000106', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000001', '1400', 'Prepaid Rent & Subscriptions', 'Advance office & warehouse payments', 'EGP', FALSE, TRUE),

-- Current Liabilities (2100)
('50000000-0000-0000-0000-000000000201', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000003', '2010', 'Trade Accounts Payable (Suppliers)', 'Commercial local vendors payable', 'EGP', FALSE, TRUE),
('50000000-0000-0000-0000-000000000202', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000003', '2050', 'ETA Value Added Tax Payable (14% VAT)', 'Egyptian Tax Authority VAT output minus input', 'EGP', FALSE, TRUE),
('50000000-0000-0000-0000-000000000203', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000003', '2060', 'Withholding Tax Payable (WHT 1%)', 'Commercial and industrial tax withheld', 'EGP', FALSE, TRUE),
('50000000-0000-0000-0000-000000000204', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000003', '2100', 'Accrued Salaries & Social Insurance (GOSI)', 'Payroll & Egyptian social insurance dues', 'EGP', FALSE, TRUE),

-- Equity (3000)
('50000000-0000-0000-0000-000000000301', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000005', '3010', 'Paid-in Capital (Capital Stock)', 'Authorized and issued company capital', 'EGP', FALSE, TRUE),
('50000000-0000-0000-0000-000000000302', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000005', '3020', 'Retained Earnings', 'Accumulated undistributed net profits', 'EGP', FALSE, TRUE),

-- Revenue (4000)
('50000000-0000-0000-0000-000000000401', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000006', '4010', 'Merchandise Sales Revenue', 'Revenue from equipment & products', 'EGP', FALSE, TRUE),
('50000000-0000-0000-0000-000000000402', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000006', '4020', 'Technical & Consulting Services Revenue', 'Implementation and maintenance fees', 'EGP', FALSE, TRUE),

-- Cost of Goods Sold (5000)
('50000000-0000-0000-0000-000000000501', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000007', '5010', 'Cost of Goods Sold (COGS)', 'Direct inventory purchase cost sold', 'EGP', FALSE, TRUE),

-- Operating Expenses (6000)
('50000000-0000-0000-0000-000000000601', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000008', '6010', 'Salaries, Allowances & Social Insurance', 'Egyptian employee payroll compensation', 'EGP', FALSE, TRUE),
('50000000-0000-0000-0000-000000000602', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000008', '6020', 'Office & Warehouse Rent Expense', 'New Cairo office and 6th October warehouse', 'EGP', FALSE, TRUE),
('50000000-0000-0000-0000-000000000603', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000008', '6030', 'Electricity, Water & Fiber Internet', 'Telecom Egypt (WE) and utility expenses', 'EGP', FALSE, TRUE),
('50000000-0000-0000-0000-000000000604', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000008', '6040', 'Marketing, Advertising & Exhibitions', 'Cairo ICT & digital campaigns', 'EGP', FALSE, TRUE),
('50000000-0000-0000-0000-000000000605', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000008', '6050', 'Bank Charges & InstaPay / POS Fees', 'Egyptian banking and gateway fees', 'EGP', FALSE, TRUE)
ON CONFLICT (company_id, account_code) DO NOTHING;

-- 8. Egyptian Banks & Warehouses
INSERT INTO bank_accounts (id, company_id, gl_account_id, bank_name, account_number, account_name, currency, branch, swift_code)
VALUES 
(
    '60000000-0000-0000-0000-000000000001',
    '00000000-0000-0000-0000-000000000001',
    '50000000-0000-0000-0000-000000000102',
    'Commercial International Bank (CIB)',
    'EG450010004500001234567890123',
    'MASA Egypt - Main Operations (EGP)',
    'EGP',
    'New Cairo Branch',
    'CIBEEGCX'
),
(
    '60000000-0000-0000-0000-000000000002',
    '00000000-0000-0000-0000-000000000001',
    '50000000-0000-0000-0000-000000000103',
    'National Bank of Egypt (NBE)',
    'EG890003008900009876543210987',
    'MASA Egypt - Treasury & Tax Account (EGP)',
    'EGP',
    '5th Settlement Branch',
    'NBEGEGCX'
)
ON CONFLICT (company_id, account_number) DO NOTHING;

INSERT INTO warehouses (id, company_id, code, name, location, is_active)
VALUES 
(
    '70000000-0000-0000-0000-000000000001',
    '00000000-0000-0000-0000-000000000001',
    'WH-6OCT',
    '6th of October Central Warehouse',
    'Industrial Zone 3, 6th of October City, Giza',
    TRUE
),
(
    '70000000-0000-0000-0000-000000000002',
    '00000000-0000-0000-0000-000000000001',
    'WH-ALEX',
    'Alexandria Hub Warehouse',
    'Borg El Arab Industrial Area, Alexandria',
    TRUE
)
ON CONFLICT (company_id, code) DO NOTHING;

-- 9. Units of Measure & Product Categories
INSERT INTO units_of_measure (id, company_id, code, name)
VALUES 
('80000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 'PCS', 'Pieces (قطعة)'),
('80000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', 'BOX', 'Carton / Box (كرتونة)'),
('80000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000001', 'HRS', 'Consulting Hours (ساعة عمل)')
ON CONFLICT (company_id, code) DO NOTHING;

INSERT INTO product_categories (id, company_id, name, description)
VALUES 
('90000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 'Enterprise Hardware & Servers', 'Laptops, Dell/HP servers, POS terminals'),
('90000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', 'Networking & Optical Fiber', 'Cisco routers, switches, fiber cabling'),
('90000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000001', 'Professional IT Services', 'ERP implementation, cloud setup, ETA e-invoice integration')
ON CONFLICT (company_id, name) DO NOTHING;

-- 10. Sample Egyptian Products
INSERT INTO products (id, company_id, category_id, uom_id, sku, name, description, cost_price, selling_price, tax_id, is_service, is_active, reorder_level)
VALUES 
(
    'c0000000-0000-0000-0000-000000000001',
    '00000000-0000-0000-0000-000000000001',
    '90000000-0000-0000-0000-000000000001',
    '80000000-0000-0000-0000-000000000001',
    'HW-DELL-5520',
    'Dell Latitude 5520 Business Laptop',
    'Core i7, 16GB RAM, 512GB SSD NVMe',
    36500.0000,
    46000.0000,
    '30000000-0000-0000-0000-000000000001',
    FALSE,
    TRUE,
    5.00
),
(
    'c0000000-0000-0000-0000-000000000002',
    '00000000-0000-0000-0000-000000000001',
    '90000000-0000-0000-0000-000000000002',
    '80000000-0000-0000-0000-000000000001',
    'NET-CISCO-C9200',
    'Cisco Catalyst 9200 24-Port Gigabit Switch',
    'Managed enterprise switch with PoE+',
    28000.0000,
    37500.0000,
    '30000000-0000-0000-0000-000000000001',
    FALSE,
    TRUE,
    3.00
),
(
    'c0000000-0000-0000-0000-000000000003',
    '00000000-0000-0000-0000-000000000001',
    '90000000-0000-0000-0000-000000000003',
    '80000000-0000-0000-0000-000000000003',
    'SRV-ETA-INTEG',
    'ETA E-Invoicing & ERP Integration Support (Hour)',
    'Egyptian Tax Authority e-Receipt & e-Invoice implementation',
    0.0000,
    1800.0000,
    '30000000-0000-0000-0000-000000000001',
    TRUE,
    TRUE,
    0.00
)
ON CONFLICT (company_id, sku) DO NOTHING;

-- 11. Sample Egyptian Customers (Clients)
INSERT INTO customers (id, company_id, code, name, company_name, tax_number, email, phone, billing_address, shipping_address, credit_limit, payment_terms_days)
VALUES 
(
    'a0000000-0000-0000-0000-000000000001',
    '00000000-0000-0000-0000-000000000001',
    'CUST-EG-001',
    'El Sewedy Electric S.A.E.',
    'El Sewedy Electric Corporation',
    '200-145-891',
    'procurement@elsewedy.com',
    '+20 2 2759 9700',
    'Plot 27, 1st Sector, 5th Settlement, New Cairo',
    '10th of Ramadan City Industrial Plant, Sharkia',
    500000.00,
    45
),
(
    'a0000000-0000-0000-0000-000000000002',
    '00000000-0000-0000-0000-000000000001',
    'CUST-EG-002',
    'Talaat Moustafa Group (TMG)',
    'Alexandria Real Estate Investment S.A.E.',
    '315-980-412',
    'finance@tmg.com.eg',
    '+20 2 3331 2000',
    '34 Mossadak St, Dokki, Giza',
    'Madinaty Project Management Office, Cairo',
    350000.00,
    30
)
ON CONFLICT (company_id, code) DO NOTHING;

-- 12. Sample Egyptian Suppliers (Vendors)
INSERT INTO suppliers (id, company_id, code, name, company_name, tax_number, email, phone, address, payment_terms_days)
VALUES 
(
    'b0000000-0000-0000-0000-000000000001',
    '00000000-0000-0000-0000-000000000001',
    'SUPP-EG-001',
    'Mantrac Egypt LLC',
    'Mantrac Unatrac International Distribution LLC',
    '410-332-901',
    'orders@mantracegypt.com',
    '+20 2 3539 0000',
    'Km 28 Cairo-Alexandria Desert Road, Smart Village',
    30
),
(
    'b0000000-0000-0000-0000-000000000002',
    '00000000-0000-0000-0000-000000000001',
    'SUPP-EG-002',
    'Telecom Egypt (WE)',
    'Telecom Egypt S.A.E.',
    '100-222-333',
    'enterprise@te.eg',
    '+20 2 3131 5555',
    'Smart Village, Building B10, Giza',
    30
)
ON CONFLICT (company_id, code) DO NOTHING;

-- 13. Sample Egyptian Expense Categories
INSERT INTO expense_categories (id, company_id, gl_account_id, name, description, is_active)
VALUES 
(
    'e0000000-0000-0000-0000-000000000001',
    '00000000-0000-0000-0000-000000000001',
    '50000000-0000-0000-0000-000000000603',
    'Utilities & Fiber Communications (Telecom Egypt)',
    'Office internet, landlines, and electric power bills',
    TRUE
),
(
    'e0000000-0000-0000-0000-000000000002',
    '00000000-0000-0000-0000-000000000001',
    '50000000-0000-0000-0000-000000000602',
    'Facility Rental & Office Maintenance',
    'New Cairo headquarters lease installments',
    TRUE
),
(
    'e0000000-0000-0000-0000-000000000003',
    '00000000-0000-0000-0000-000000000001',
    '50000000-0000-0000-0000-000000000605',
    'Bank Commissions & InstaPay / CIB Fees',
    'Transfer charges and commercial payment processing',
    TRUE
)
ON CONFLICT (company_id, name) DO NOTHING;
