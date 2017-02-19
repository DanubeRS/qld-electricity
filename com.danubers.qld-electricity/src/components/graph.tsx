import {State} from "../store/index";
import {EnergyData} from "../types/data/energyData";
import {connect, Dispatch, MapDispatchToPropsFunction, MapStateToProps} from "react-redux";
import * as Chart from "chart.js";
import * as React from "react";
import * as ReactDOM from "react-dom";
/**
 * Created by daniel on 19/2/17.
 */

const mapStateToProps : MapStateToProps<StateProps, any> = (state: State) => (
    {data: state.data.energy}
);

const mapDispatchToProps : MapDispatchToPropsFunction<DispatchProps, any> = (dispatch, ownProps) => (
    {dispatch: dispatch}
);

interface StateProps {
    data: EnergyData
}

interface DispatchProps {
    dispatch: Dispatch<any>
}

export class GraphComponent extends React.Component<DispatchProps & StateProps, {}> {
    private _lineChart: Chart = null;


    componentDidUpdate(prevProps: DispatchProps & StateProps, prevState: {}, prevContext: any): void {
    }

    render(): JSX.Element | any {
        return <canvas width={400} height={400}></canvas>
    }

    //Render chart data
    componentDidMount(): void {
        let ctx = (ReactDOM.findDOMNode(this) as HTMLCanvasElement).getContext('2d');
        let myChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: ["Red", "Blue", "Yellow", "Green", "Purple", "Orange"],
                datasets: [{
                    label: '# of Votes',
                    data: [12, 19, 3, 5, 2, 3],
                    backgroundColor: [
                        'rgba(255, 99, 132, 0.2)',
                        'rgba(54, 162, 235, 0.2)',
                        'rgba(255, 206, 86, 0.2)',
                        'rgba(75, 192, 192, 0.2)',
                        'rgba(153, 102, 255, 0.2)',
                        'rgba(255, 159, 64, 0.2)'
                    ],
                    borderColor: [
                        'rgba(255,99,132,1)',
                        'rgba(54, 162, 235, 1)',
                        'rgba(255, 206, 86, 1)',
                        'rgba(75, 192, 192, 1)',
                        'rgba(153, 102, 255, 1)',
                        'rgba(255, 159, 64, 1)'
                    ],
                    borderWidth: 1
                }]
            },
            options: {
                scales: {
                    yAxes: [{
                        ticks: {
                        }
                    }]
                },
                responsive: false
            }
        });
    }
}

export default connect(mapStateToProps, mapDispatchToProps)(GraphComponent);
