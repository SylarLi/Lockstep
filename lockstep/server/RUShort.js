//# 16位位标记

var MinValue = 0;
var MaxValue = 0xff;
var MaxValue2 = parseInt(MaxValue / 2);

function RUShort(value) {
	this.value = value;
}

// >
RUShort.prototype.laterThan = function(val) {
	return (this.value > val.value && (this.value - val.value) <= MaxValue2) ||
		(this.value < val.value && (val.value - this.value) >= MaxValue2);
};

// <
RUShort.prototype.before = function(val) {
	return val.laterThan(this);
}

// +
RUShort.prototype.add = function(val) {
	var value = this.value + val.value;
    return new RUShort(value > MaxValue ? (value - MaxValue - 1) : value);
};

// 距离值，始终为正值
RUShort.prototype.minus = function(val) {
	return new RUShort(this.value >= val.value ? (this.value - val.value) : (this.value + MaxValue + 1 - val.value));
};

// ++
RUShort.prototype.add$$ = function() {
	this.value = this.value == MaxValue ? MinValue : ++this.value;
	return this;
}

module.exports = RUShort;
module.exports.MinValue = MinValue;
module.exports.MaxValue = MaxValue;