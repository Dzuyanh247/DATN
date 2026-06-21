-- Diagnostic SELECTs only: inspect legacy NULL data that may cause SqlNullValueException/NullReferenceException.
SELECT 'Orders' AS TableName, COUNT(*) AS NullRiskRows
FROM Orders
WHERE ReceiverName IS NULL OR ReceiverPhone IS NULL OR ShippingAddress IS NULL OR PaymentStatus IS NULL;

SELECT 'OrderDetails' AS TableName, COUNT(*) AS NullRiskRows
FROM OrderDetails
WHERE ProductName IS NULL OR ProductImage IS NULL OR Warranty IS NULL;

SELECT 'WarrantyRequests' AS TableName, COUNT(*) AS NullRiskRows
FROM WarrantyRequests
WHERE CustomerName IS NULL OR Phone IS NULL OR WarrantyCode IS NULL OR RequestCode IS NULL OR ProductName IS NULL OR Status IS NULL;

SELECT 'Products' AS TableName, COUNT(*) AS NullRiskRows
FROM Products
WHERE Name IS NULL OR ProductCode IS NULL OR Slug IS NULL;

SELECT 'Users' AS TableName, COUNT(*) AS NullRiskRows
FROM Users
WHERE FullName IS NULL OR Email IS NULL OR Username IS NULL;
