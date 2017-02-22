"use strict";
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var DataStore = require("../store/reducers/data");
var react_redux_1 = require("react-redux");
var Chart = require("chart.js");
var React = require("react");
var ReactDOM = require("react-dom");
var api_1 = require("./../api");
/**
 * Created by daniel on 19/2/17.
 */
var mapStateToProps = function (state) { return ({ energy: state.data.energy, weather: state.data.weather }); };
var mapDispatchToProps = function (dispatch, ownProps) { return ({ dispatch: dispatch }); };
function mapEnergyData(model) {
    var mapped = {
        start: model.startTime,
        end: model.endTime,
        points: model.nodes.map(function (node) { return ({
            timestamp: node.timestamp,
            value: node.value.instantaneous
        }); })
    };
    return Promise.resolve(mapped);
}
function mapWeatherData(model) {
    var mapped = {
        start: model.startTime,
        end: model.endTime,
        points: model.nodes.map(function (node) { return ({
            timestamp: node.timestamp,
            value: node.value.airTemp
        }); })
    };
    return Promise.resolve(mapped);
}
var GraphComponent = (function (_super) {
    __extends(GraphComponent, _super);
    function GraphComponent() {
        var _this = _super !== null && _super.apply(this, arguments) || this;
        _this._ctx = null;
        _this._chart = null;
        return _this;
    }
    GraphComponent.prototype.componentDidUpdate = function (prevProps, prevState, prevContext) {
        if (prevProps !== this.props) {
            if (prevProps.weather === null && prevProps.energy === null)
                this.setGraphData();
            this.updateGraphData();
        }
    };
    GraphComponent.prototype.render = function () {
        var _this = this;
        return <div className="energy-chart">
            <button type="button" onClick={function (e) { return _this.updateChart(); }}>Update chart</button>
            <canvas width={800} height={800}></canvas>
        </div>;
    };
    //Render chart data
    GraphComponent.prototype.componentDidMount = function () {
        this._ctx = ReactDOM.findDOMNode(this).getElementsByTagName('canvas')[0].getContext('2d');
        //Load in data from the api
        this.updateChart();
    };
    GraphComponent.prototype.setGraphData = function () {
        if (!this._ctx) {
            throw new Error();
        }
        this._chart = new Chart(this._ctx, {
            type: 'line',
            data: {
                datasets: this.mapDataset()
            },
            options: {
                scales: {
                    xAxes: [{
                            type: 'time',
                            id: 'time',
                            position: 'bottom',
                            time: {
                                displayFormats: {
                                    quarter: 'h:mm:ss a'
                                }
                            }
                        }],
                    yAxes: [
                        { id: 'energy', scaleLabel: 'Energy (MWh)', position: 'left', display: true, type: 'linear' },
                        { id: 'temperature', scaleLabel: 'Temperature (c)', position: 'right', display: true, type: 'linear', ticks: {
                                min: -10, max: 50
                            } }
                    ]
                }, responsive: false,
            }
        });
    };
    GraphComponent.prototype.mapDataset = function () {
        var dataset = [];
        var test = false;
        if (this.props.energy) {
            dataset.push({
                label: "Energex",
                backgroundColor: 'green',
                data: (this.props.energy || { points: [] }).points.map(function (p) { return ({ x: p.timestamp, y: p.value }); }),
                lineTension: 0.5,
                fill: false,
                borderWidth: 5,
                borderColor: 'green',
                pointRadius: 0,
                pointHitRadius: 5,
                yAxisID: 'energy',
                xAxisID: 'time'
            });
        }
        if (this.props.weather) {
            dataset.push({
                label: "Temperature",
                backgroundColor: 'red',
                data: (this.props.weather || { points: [] }).points.map(function (p) { return ({ x: p.timestamp, y: p.value }); }),
                lineTension: 0.5,
                fill: false,
                borderWidth: 5,
                borderColor: 'red',
                pointRadius: 0,
                pointHitRadius: 5,
                yAxisID: 'temperature',
                xAxisID: 'time'
            });
        }
        return dataset;
    };
    GraphComponent.prototype.updateGraphData = function () {
        var _this = this;
        // (this._chart.data as LinearChartData).datasets = this.mapDataset();
        var newDataset = this.mapDataset();
        this._chart.data.datasets.forEach(function (dataset, index, array) {
            //Check if dataset exists in array
            if (newDataset.every(function (ds) { return ds.label !== dataset.label; })) {
                array.splice(index, 1);
            }
            var newMatched = newDataset.find(function (ds) { return ds.label === dataset.label; });
            //remove
            dataset.data.forEach(function (dataNode, index, newArray) {
                if (newMatched.data.every(function (nds) { return nds.x !== dataNode.x; }))
                    newArray.splice(index, 1);
            });
            //add
            newMatched.data.forEach(function (newNode, index, array) {
                if (dataset.data.every(function (dsn) { return dsn.x !== newNode.x; })) {
                    dataset.data.push(newNode);
                }
            });
            //update
            newMatched.data.forEach(function (newNode, index, array) {
                var matched = dataset.data.find(function (dsn) {
                    return dsn.x === newNode.x;
                });
                matched.y = newNode.y;
            });
        });
        //Add new datasets
        newDataset.forEach(function (ds) {
            if (_this._chart.data.datasets.every(function (eds) { return eds.label !== ds.label; })) {
                _this._chart.data.datasets.push(ds);
            }
        });
        this._chart.update();
    };
    GraphComponent.prototype.updateChart = function () {
        var _this = this;
        var api = new api_1.Client();
        this.props.dispatch(DataStore.loadingEnergyData(true));
        var today = new Date();
        var yesterday = new Date();
        yesterday.setDate(today.getDate() - 1);
        api.apiDataPowerGet(yesterday, today).then(function (m) {
            return mapEnergyData(m).then(function (d) { return _this.props.dispatch(DataStore.loadedEnergyData(d)); });
        }).then(function (p) {
            return api.apiDataWeatherByStationIdGet("1", yesterday, today);
        }).then(function (m) {
            return mapWeatherData(m).then(function (d) { return _this.props.dispatch(DataStore.loadedWeatherData(d)); });
        });
    };
    return GraphComponent;
}(React.Component));
exports.GraphComponent = GraphComponent;
Object.defineProperty(exports, "__esModule", { value: true });
exports.default = react_redux_1.connect(mapStateToProps, mapDispatchToProps)(GraphComponent);
