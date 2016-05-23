//# LockStep服务器环境上下文

var RUShort = require('./RUShort.js');

function LockStepEnv () {
	this.frame = new RUShort(0);
	this.start = false;
	this.from = new RUShort(0);						// 当前可以发送(尚未发送)的最小齐备包帧序号
	this.to = new RUShort(RUShort.MaxValue);		// 当前可以发送(尚未发送)的最大齐备包帧序号
}

module.exports = LockStepEnv;