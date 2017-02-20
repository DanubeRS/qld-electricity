import {State} from "../store/index";
import * as DataStore from '../store/reducers/data'
import {EnergyData} from "../types/data/energyData";
import {connect, Dispatch, MapDispatchToPropsFunction, MapStateToProps} from "react-redux";
import * as Chart from "chart.js";
import * as React from "react";
import * as ReactDOM from "react-dom";
import {Client, PowerDataResponseModel} from './../api';
import {EnergyDataPoint} from "../types/data/energyDataPoint";
import {ChartDataSets, ChartPoint, LinearChartData} from "chart.js";
/**
 * Created by daniel on 19/2/17.
 */

const mapStateToProps: MapStateToProps<StateProps, any> = (state: State) => (
    {data: state.data.energy}
);

const mapDispatchToProps: MapDispatchToPropsFunction<DispatchProps, any> = (dispatch, ownProps) => (
    {dispatch: dispatch}
);

interface StateProps {
    data: EnergyData
}

interface DispatchProps {
    dispatch: Dispatch<any>
}

function mapEnergyData(model: PowerDataResponseModel): Promise<EnergyData> {
    let mapped = {
        start: model.startTime,
        end: model.endTime,
        points: model.nodes.map(node => ({
            timestamp: node.timestamp,
            value: node.value.instantaneous
        } as EnergyDataPoint))
    } as EnergyData;
    return Promise.resolve(mapped);
}
export class GraphComponent extends React.Component<DispatchProps & StateProps, {}> {
    private _ctx: CanvasRenderingContext2D = null;
    private _chart: Chart = null;

    componentDidUpdate(prevProps: DispatchProps & StateProps, prevState: {}, prevContext: any): void {
        if (prevProps.data !== this.props.data) {
            if (prevProps.data === null)
                this.setGraphData();
            this.updateGraphData();
        }
    }

    render(): JSX.Element | any {
        return <div className="energy-chart">
            <button type="button" onClick={e => this.updateChart()}>Update chart</button>
            <canvas width={400} height={400}></canvas>
        </div>
    }

    //Render chart data
    componentDidMount(): void {
        this._ctx = ((ReactDOM.findDOMNode(this) as HTMLElement).getElementsByTagName('canvas')[0] as HTMLCanvasElement).getContext('2d');

        //Load in data from the api
        this.updateChart();
    }

    private setGraphData() {
        if (!this._ctx) {
            throw new Error(/*TODO*/);
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
                        position: 'bottom',
                        time: {
                            displayFormats: {
                                quarter: 'h:mm:ss a'
                            }
                        }
                    }]
                }, responsive: false
            }
        })
    }

    private mapDataset(): ChartDataSets[] {
        return [{
            label: "Energex",
            backgroundColor: 'green',
            data: this.props.data.points.map((p: EnergyDataPoint) => ({x: p.timestamp, y: p.value} as ChartPoint))
        }];
    }

    private updateGraphData() {
        // (this._chart.data as LinearChartData).datasets = this.mapDataset();
        let newDataset = this.mapDataset();
        (this._chart.data as LinearChartData).datasets.forEach((dataset, index, array) => {
            //Check if dataset exists in array
            if (newDataset.every(ds => ds.label !== dataset.label)) {
                array.splice(index, 1);
            }
            let newMatched = newDataset.find(ds => ds.label === dataset.label);

            //remove
            (dataset.data as ChartPoint[]).forEach((dataNode, index, newArray) => {
                if ((newMatched.data as ChartPoint[]).every(nds => nds.x !== dataNode.x))
                    newArray.splice(index, 1);
            });
            //add
            (newMatched.data as ChartPoint[]).forEach((newNode, index, array) => {
                if ((dataset.data as ChartPoint[]).every(dsn => dsn.x !== newNode.x)) {
                    (dataset.data as ChartPoint[]).push(newNode);
                }
            });
            //update
            (newMatched.data as ChartPoint[]).forEach((newNode, index, array) => {
                let matched = (dataset.data as ChartPoint[]).find(dsn =>
                dsn.x === newNode.x);
                matched.y = newNode.y;
            });
        });
        this._chart.update();
    }

    private updateChart() {
        let api = new Client();
        this.props.dispatch(DataStore.loadingEnergyData(true));
        let today = new Date();
        let yesterday = new Date();
        yesterday.setDate(today.getDate() - 1);
        api.apiDataPowerGet(yesterday, today).then(m => {
            return mapEnergyData(m).then(d => this.props.dispatch(DataStore.loadedEnergyData(d)))
        })
    }
}

export default connect(mapStateToProps, mapDispatchToProps)(GraphComponent);
