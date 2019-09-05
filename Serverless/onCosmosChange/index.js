const Connection = require('tedious').Connection;
const Types = require('tedious').TYPES;

module.exports = function (context, input) {
    const log = context.log;
    log.info(`Received ${input.length} documents. First document Id: ${input[0].id}`);
    const config = {
        authentication: {
            options: {
                userName: process.env["SqlServerUser"],
                password: process.env["SqlServerPassword"]
            },
            type: 'default'
        },
        server: process.env["SqlServerAddress"],
        options: {
            database: 'test', 
            encrypt: true,
            useUTC: true
        }
    }
    // configure and open connection to SQL Azure
    log.info('Trying to connect to SQL server...');
    const connection = new Connection(config);    
    connection.on('connect', function(err) {
        if (err) {   
            // error during connection establishment             
            context.done(err, null);
        } else {
            // connection established
            log.info('Connected to SQL server.');
            // define and prepare bulk load
            const bulkOptions = { 
                keepNulls: true 
            };
            var bulkLoad = connection.newBulkLoad('Orders', bulkOptions, function (err, rowCount) {
                if (err) {
                    log.error(err);                    
                } else {
                    log.info(`Inserted ${rowCount} rows.`);                    
                }
                connection.close();
                context.done(err, rowCount > 0);
            });            
            try {
                bulkLoad.addColumn('ProductId', Types.BigInt, { nullable: false });
                bulkLoad.addColumn('OrderDate', Types.VarChar, { scale: 7, nullable: true });
                bulkLoad.addColumn('CustomerLogin', Types.NVarChar, { length: 500, nullable: false });            
                bulkLoad.addColumn('Amount', Types.Int, { nullable: false });            
                // add row for each input element
                log.info(`Adding ${input.length} rows to bulk loader...`);           
                input.forEach(function(o) {
                    bulkLoad.addRow({ 
                        ProductId: o.productId, 
                        OrderDate: o.timeStamp,
                        CustomerLogin: o.customerLogin,
                        Amount: o.amount 
                    });            
                });
            } catch (ex) {
                context.done(ex, null);
            }
            log.info('Executing request...');
            connection.execBulkLoad(bulkLoad);
        }
    });
};