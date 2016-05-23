//# 客户端信息

var RUShort = require('./RUShort.js');

var NetState = {
	None : 0,
	Connecting : 1,
	Connected : 2
}

function Client(id) {

	// Client base info
	this.id = id;
	this.index = -1;
	this.address = null;
	this.port = 0;
	this.sequence = new RUShort(0);
	this.ack = new RUShort(0);
	this.ackbit = 0;
	this.quantics = {};					// key ushort : value Packet
	this.lastReceviedTime = 0;			// in millseconds
	this.netState = NetState.None;

	// LockStep
	this.frameDatas = {};

	this.connectTime = 0;				// 起始连接时间
	this.connectedTime = 0;				// 连接成功时间
	this.disconnectTime = 0;			// 断开连接时间

	// 临时变量
	this.lastConnectTime = 0;
	this.connectTimes = 0;
}

Client.prototype.setConnecting = function() {
	this.netState = NetState.Connecting;
	this.connectTime = Date.now();
	this.connectedTime = 0;
	this.disconnectTime = 0;
	console.log('Client ' + this.id + ' connecting' + ' ' + (new Date(this.connectTime)));
};

Client.prototype.setConnected = function() {
	this.netState = NetState.Connected;
	this.connectedTime = Date.now();
	console.log('Client ' + this.id + ' connected' + ' ' + (new Date(this.connectedTime)));
}

Client.prototype.setDisconnected = function() {
	this.netState = NetState.None;
	this.sequence = new RUShort(0);
	this.ack = new RUShort(0);
	this.ackbit = 0;
	this.quantics = {};	
	this.lastReceviedTime = 0;
	this.disconnectTime = Date.now();
	this.lastConnectTime = 0;
	this.connectTimes = 0;

	if (this.connectedTime > 0) {
		var tseconds = parseInt((this.disconnectTime - this.connectedTime) / 1000);
		var seconds = tseconds % 60;
		var tminutes = parseInt(tseconds / 60);
		var minutes = tminutes % 60;
		var thours = parseInt(tminutes / 60);
		console.log('Client ' + this.id + ' online duration ' + thours + ':' + minutes + ':' + seconds);
	}
	console.log('Client ' + this.id + ' lose connect' + ' ' + (new Date(this.disconnectTime)) + '\n');
}

Client.prototype.toString = function() {
	return 'id : ' + this.id + '\n' + 
		   'address : ' + this.address + '\n' +
	   	   'port : ' + this.port + '\n' + 
	   	   'netState : ' + this.netState + '\n';
};

module.exports = Client;
module.exports.NetState = NetState;

